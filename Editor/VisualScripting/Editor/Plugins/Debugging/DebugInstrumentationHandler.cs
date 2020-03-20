using System.Collections.Generic;
using System.Diagnostics;
using UnityEditor.Experimental.GraphView;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.VisualScripting.Editor.Plugins
{
    public class DebugInstrumentationHandler : IPluginHandler
    {
        const string k_TraceHighlight = "trace-highlight";
        const string k_ExceptionHighlight = "exception-highlight";
        const string k_TraceSecondaryHighlight = "trace-secondary-highlight";
        const int k_UpdateIntervalMs = 10;

        VisualElement m_IconsParent;
        Store m_Store;
        GraphView m_GraphView;
        List<TracingStep> GraphDebuggingData => m_Store.GetState()?.DebuggingData;
        PauseState m_PauseState = PauseState.Unpaused;
        PlayModeStateChange m_PlayState = PlayModeStateChange.EnteredEditMode;
        int? m_CurrentFrame;
        int? m_CurrentStep;
        int? m_CurrentTarget;
        Stopwatch m_Stopwatch;
        bool m_ForceUpdate;

        public void Register(Store store, GraphView graphView)
        {
            m_Store = store;
            m_GraphView = graphView;
            EditorApplication.update += OnUpdate;
            EditorApplication.pauseStateChanged += OnEditorPauseStateChanged;
            EditorApplication.playModeStateChanged += OnEditorPlayModeStateChanged;
            m_Store.StateChanged += OnStateChangeUpdate;
            m_Store.GetState().CurrentGraphModel?.Stencil?.Debugger?.Start();
        }

        public void Unregister()
        {
            ClearHighlights();
            // ReSharper disable once DelegateSubtraction
            EditorApplication.update -= OnUpdate;
            EditorApplication.pauseStateChanged -= OnEditorPauseStateChanged;
            EditorApplication.playModeStateChanged -= OnEditorPlayModeStateChanged;
            m_Store.StateChanged -= OnStateChangeUpdate;
            m_Store.GetState().CurrentGraphModel?.Stencil?.Debugger?.Stop();
        }

        public void OptionsMenu(GenericMenu menu) {}

        void OnStateChangeUpdate()
        {
            m_ForceUpdate = true;
            OnUpdate();
            m_ForceUpdate = false;
        }

        void OnUpdate()
        {
            if (!IsDirty())
                return;

            MapDebuggingData();
        }

        void OnEditorPauseStateChanged(PauseState state)
        {
            m_PauseState = state;

            // TODO Save tracing data
        }

        void OnEditorPlayModeStateChanged(PlayModeStateChange state)
        {
            m_PlayState = state;

            if (m_PlayState == PlayModeStateChange.ExitingPlayMode)
            {
                ClearHighlights();
                m_Stopwatch?.Stop();
                m_Stopwatch = null;
            }
        }

        void MapDebuggingData()
        {
            bool needUpdate = false;
            if (m_Stopwatch == null)
                m_Stopwatch = Stopwatch.StartNew();
            else if (EditorApplication.isPaused || m_Stopwatch.ElapsedMilliseconds > k_UpdateIntervalMs)
            {
                needUpdate = true;
                m_Stopwatch.Restart();
            }

            if (needUpdate)
                ClearHighlights();

            var state = m_Store.GetState();
            var currentGraphModel = state?.CurrentGraphModel;
            var graph = currentGraphModel?.AssetModel;
            var debugger = graph?.GraphModel?.Stencil?.Debugger;
            if (state == null || debugger == null)
                return;

            if (needUpdate && debugger.GetTracingSteps(currentGraphModel, state.CurrentTracingFrame, state.CurrentTracingTarget,
                out var stepList))
            {
                state.MaxTracingStep = stepList == null ? 0 : stepList.Count;
                state.DebuggingData = stepList;
                HighlightTrace();
            }
        }

        void ClearHighlights()
        {
            VseGraphView gv = (VseGraphView)m_GraphView;
            if (gv?.UIController.ModelsToNodeMapping != null)
            {
                foreach (GraphElement x in gv.UIController.ModelsToNodeMapping.Values)
                {
                    x.RemoveFromClassList(k_TraceHighlight);
                    x.RemoveFromClassList(k_TraceSecondaryHighlight);
                    x.RemoveFromClassList(k_ExceptionHighlight);

                    VseUIController.ClearErrorBadge(x);
                    if (x is Node n)
                        VseUIController.ClearPorts(n);

                    // TODO ugly
                    gv.UIController.DisplayCompilationErrors(gv.store.GetState());
                }
            }
        }

        void HighlightTrace()
        {
            Dictionary<IGraphElementModel, GraphElement> modelsToNodeUiMapping =
                ((VseGraphView)m_GraphView).UIController.ModelsToNodeMapping;
            if (GraphDebuggingData != null)
            {
                var currentTracingStep = m_Store.GetState().CurrentTracingStep;
                if (currentTracingStep < 0 || currentTracingStep >= GraphDebuggingData.Count)
                {
                    m_Store.GetState().CurrentTracingStep = -1;
                    foreach (TracingStep step in GraphDebuggingData)
                    {
                        AddStyleClassToModel(step, modelsToNodeUiMapping, k_TraceHighlight);
                        DisplayStepValues(step, modelsToNodeUiMapping);
                    }
                }
                else
                {
                    for (var i = 0; i < currentTracingStep; i++)
                    {
                        var step = GraphDebuggingData[i];
                        AddStyleClassToModel(step, modelsToNodeUiMapping, k_TraceHighlight);
                        DisplayStepValues(step, modelsToNodeUiMapping);
                    }
                }
            }
        }

        void DisplayStepValues(TracingStep step, Dictionary<IGraphElementModel, GraphElement> modelsToNodeUiMapping)
        {
            switch (step.Type)
            {
                case TracingStepType.ExecutedNode:
                    // Do Nothing, already handled in HighlightTrace()
                    break;
                case TracingStepType.TriggeredPort:
                    var p = GetPortFromPortModel(step.PortModel, (Experimental.GraphView.Node)modelsToNodeUiMapping[step.NodeModel]);
                    p.ExecutionPortActive = true;
                    break;
                case TracingStepType.WrittenValue:
                    AddValueToPort(modelsToNodeUiMapping, step.PortModel, step.ValueString);
                    break;
                case TracingStepType.ReadValue:
                    AddValueToPort(modelsToNodeUiMapping, step.PortModel, step.ValueString);
                    break;
            }

            if (step.NodeModel.HasProgress && modelsToNodeUiMapping.TryGetValue(step.NodeModel, out var node) && node is Node vsNode)
            {
                vsNode.Progress = step.Progress;
            }
        }

        void AddValueToPort(IReadOnlyDictionary<IGraphElementModel, GraphElement> modelsToNodeUiMapping, IPortModel port, string valueReadableValue)
        {
            if (port != null && modelsToNodeUiMapping.TryGetValue(port.NodeModel, out GraphElement nodeUi))
            {
                if (m_PauseState == PauseState.Paused || m_PlayState == PlayModeStateChange.EnteredEditMode)
                {
                    var p = GetPortFromPortModel(port, (Experimental.GraphView.Node)nodeUi);
                    if (p == null)
                        return;
                    VisualElement cap = p.Q(className: "connectorCap");
                    ((VseGraphView)m_GraphView).UIController.AttachValue(p, cap, valueReadableValue, p.portColor, port.Direction == Direction.Output ? SpriteAlignment.BottomRight : SpriteAlignment.BottomLeft);
                }
            }
        }

        private static Port GetPortFromPortModel(IPortModel port, Experimental.GraphView.Node nodeUi)
        {
            Experimental.GraphView.Node n = nodeUi;
            var portContainer = port.Direction == Direction.Output ? n.outputContainer : n.inputContainer;
            Port p = portContainer.childCount > 0
                ? portContainer.Query<Port>().Where(x => x.Model == port).First()
                : null;
            return p;
        }

        void AddStyleClassToModel(TracingStep step, IReadOnlyDictionary<IGraphElementModel, GraphElement> modelsToNodeUiMapping, string highlightStyle)
        {
            if (step.NodeModel != null && modelsToNodeUiMapping.TryGetValue(step.NodeModel, out GraphElement ui))
            {
                // TODO TRACING errors
                // if (step.type == DebuggerTracer.EntityFrameTrace.StepType.Exception)
                // {
                //     ui.AddToClassList(k_ExceptionHighlight);
                //
                //     if (m_PauseState == PauseState.Paused || m_PlayState == PlayModeStateChange.EnteredEditMode)
                //     {
                //         ((VseGraphView)m_GraphView).UIController.AttachErrorBadge(ui, step.errorMessage, SpriteAlignment.TopLeft);
                //     }
                // }
                // else
                {
                    ui.AddToClassList(highlightStyle);
                }
            }
        }

        bool IsDirty()
        {
            bool dirty = m_CurrentFrame != m_Store.GetState().CurrentTracingFrame
                || m_CurrentStep != m_Store.GetState().CurrentTracingStep
                || m_CurrentTarget != m_Store.GetState().CurrentTracingTarget
                || m_ForceUpdate;

            m_CurrentFrame = m_Store.GetState().CurrentTracingFrame;
            m_CurrentStep = m_Store.GetState().CurrentTracingStep;
            m_CurrentTarget = m_Store.GetState().CurrentTracingTarget;

            return dirty;
        }
    }
}
