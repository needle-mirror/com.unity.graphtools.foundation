using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive.GraphElements;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using UnityEditor.Searcher;
using UnityEngine;
using UnityEngine.UIElements;
using Debug = UnityEngine.Debug;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.Plugins
{
    public class DebugInstrumentationHandler : IPluginHandler
    {
        const string k_TraceHighlight = "trace-highlight";
        const string k_ExceptionHighlight = "exception-highlight";
        const string k_TraceSecondaryHighlight = "trace-secondary-highlight";
        const int k_UpdateIntervalMs = 10;

        VisualElement m_IconsParent;
        Overdrive.Store m_Store;
        GraphView m_GraphView;
        List<TracingStep> GraphDebuggingData => m_Store.GetState()?.TracingDataModel.DebuggingData;
        PauseState m_PauseState = PauseState.Unpaused;
        PlayModeStateChange m_PlayState = PlayModeStateChange.EnteredEditMode;
        int? m_CurrentFrame;
        int? m_CurrentStep;
        int? m_CurrentTarget;
        Stopwatch m_Stopwatch;
        bool m_ForceUpdate;

        TracingToolbar m_TimelineToolbar;

        public void Register(Overdrive.Store store, GraphViewEditorWindow window)
        {
            m_Store = store;
            m_GraphView = window.GraphViews.First();
            EditorApplication.update += OnUpdate;
            EditorApplication.pauseStateChanged += OnEditorPauseStateChanged;
            EditorApplication.playModeStateChanged += OnEditorPlayModeStateChanged;
            m_Store.StateChanged += OnStateChangeUpdate;
            m_Store.GetState().CurrentGraphModel?.Stencil?.Debugger?.Start(m_Store.GetState().CurrentGraphModel, m_Store.GetState().EditorDataModel.TracingEnabled);

            var root = window.rootVisualElement;
            if (m_TimelineToolbar == null)
            {
                m_TimelineToolbar = root.Q<TracingToolbar>();
                if (m_TimelineToolbar == null)
                {
                    m_TimelineToolbar = new TracingToolbar(m_GraphView, m_Store);
                }
            }

            if (m_TimelineToolbar.parent != root)
                root.Insert(1, m_TimelineToolbar);
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
            m_TimelineToolbar?.RemoveFromHierarchy();
        }

        public void OptionsMenu(GenericMenu menu)
        {
            menu.AddItem(new GUIContent("Dump Frame Trace"), false, DumpFrameTrace);
        }

        void DumpFrameTrace()
        {
            var state = m_Store.GetState();
            var currentGraphModel = state?.CurrentGraphModel;
            var debugger = currentGraphModel?.Stencil?.Debugger;
            if (state == null || debugger == null)
                return;

            if (debugger.GetTracingSteps(currentGraphModel, state.TracingDataModel.CurrentTracingFrame, state.TracingDataModel.CurrentTracingTarget,
                out var stepList))
            {
                try
                {
                    var searcherItems = stepList.Select(MakeStepItem).ToList();
                    SearcherWindow.Show(EditorWindow.focusedWindow, searcherItems, "Steps", item =>
                    {
                        if (item != null)
                            state.TracingDataModel.CurrentTracingStep = ((StepSearcherItem)item).Index;
                        return true;
                    }, Vector2.zero);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
            else
                Debug.Log("No frame data");

            SearcherItem MakeStepItem(TracingStep step, int i)
            {
                return new StepSearcherItem(step, i);
            }
        }

        class StepSearcherItem : SearcherItem
        {
            public readonly TracingStep Step;
            public readonly int Index;

            public StepSearcherItem(TracingStep step, int i) : base(GetName(step), "asdasd")
            {
                Step = step;
                Index = i;
            }

            static string GetName(TracingStep step)
            {
                return $"{step.Type} {step.NodeModel} {step.PortModel}";
            }
        }

        void OnStateChangeUpdate()
        {
            m_ForceUpdate = true;
            OnUpdate();
            m_ForceUpdate = false;
        }

        void OnUpdate()
        {
            if (m_TimelineToolbar == null)
                return;
            if (EditorApplication.isPlaying && !EditorApplication.isPaused)
            {
                m_Store.GetState().TracingDataModel.CurrentTracingFrame = Time.frameCount;
            }

            m_TimelineToolbar.UpdateTracingMenu();

            m_TimelineToolbar?.SyncVisible();
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
            var debugger = currentGraphModel?.Stencil?.Debugger;
            if (state == null || debugger == null)
                return;

            if (needUpdate && debugger.GetTracingSteps(currentGraphModel, state.TracingDataModel.CurrentTracingFrame, state.TracingDataModel.CurrentTracingTarget,
                out var stepList))
            {
                state.TracingDataModel.MaxTracingStep = stepList == null ? 0 : stepList.Count;
                state.TracingDataModel.DebuggingData = stepList;
                HighlightTrace();
            }
        }

        void ClearHighlights()
        {
            VseGraphView gv = (VseGraphView)m_GraphView;
            foreach (GraphElements.Node x in m_GraphView.nodes.ToList())
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

        void HighlightTrace()
        {
            if (GraphDebuggingData != null)
            {
                var currentTracingStep = m_Store.GetState().TracingDataModel.CurrentTracingStep;
                if (currentTracingStep < 0 || currentTracingStep >= GraphDebuggingData.Count)
                {
                    m_Store.GetState().TracingDataModel.CurrentTracingStep = -1;
                    foreach (TracingStep step in GraphDebuggingData)
                    {
                        AddStyleClassToModel(step, k_TraceHighlight);
                        DisplayStepValues(step);
                    }
                }
                else
                {
                    for (var i = 0; i < currentTracingStep; i++)
                    {
                        var step = GraphDebuggingData[i];
                        AddStyleClassToModel(step, k_TraceHighlight);
                        DisplayStepValues(step);
                    }
                }
            }
            m_GraphView.schedule.Execute(() =>
                m_GraphView.edges.ForEach(e => e.MarkDirtyRepaint())).StartingIn(1);
        }

        void DisplayStepValues(TracingStep step)
        {
            switch (step.Type)
            {
                case TracingStepType.ExecutedNode:
                    // Do Nothing, already handled in HighlightTrace()
                    break;
                case TracingStepType.TriggeredPort:
                    var p = step.PortModel.GetUI<Port>(m_GraphView);
                    if (p != null)
                        p.ExecutionPortActive = true;
                    break;
                case TracingStepType.WrittenValue:
                    AddValueToPort(step.PortModel, step.ValueString);
                    break;
                case TracingStepType.ReadValue:
                    AddValueToPort(step.PortModel, step.ValueString);
                    break;
                case TracingStepType.Error:
                    var element = step.NodeModel.GetUI<GraphElements.Node>(m_GraphView);
                    if (element is IBadgeContainer)
                        ((VseGraphView)m_GraphView).UIController.AttachErrorBadge(element, step.ErrorMessage, SpriteAlignment.RightCenter);
                    break;
            }

            var node = step.NodeModel.GetUI<GraphElements.Node>(m_GraphView);
            if ((step.NodeModel?.HasProgress ?? false) && node != null && node is Node vsNode)
            {
                vsNode.Progress = step.Progress;
            }
        }

        void AddValueToPort(IGTFPortModel port, string valueReadableValue)
        {
            var node = port?.NodeModel.GetUI<GraphElements.Node>(m_GraphView);
            if (port != null && node != null)
            {
                if (m_PauseState == PauseState.Paused || m_PlayState == PlayModeStateChange.EnteredEditMode)
                {
                    var p = port.GetUI<GraphElements.Port>(m_GraphView);
                    if (!(p is IBadgeContainer badgeContainer))
                        return;
                    VisualElement cap = p.Q(className: "ge-port__cap") ?? p;
                    var color = p.PortColor;
                    ((VseGraphView)m_GraphView).UIController.AttachValue(badgeContainer, cap, valueReadableValue, color, port.Direction == Direction.Output ? SpriteAlignment.BottomRight : SpriteAlignment.BottomLeft);
                }
            }
        }

        void AddStyleClassToModel(TracingStep step, string highlightStyle)
        {
            var node = step.NodeModel.GetUI<Node>(m_GraphView);
            if (step.NodeModel != null && node != null)
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
                    node.AddToClassList(highlightStyle);
                }
            }
        }

        bool IsDirty()
        {
            bool dirty = m_CurrentFrame != m_Store.GetState().TracingDataModel.CurrentTracingFrame
                || m_CurrentStep != m_Store.GetState().TracingDataModel.CurrentTracingStep
                || m_CurrentTarget != m_Store.GetState().TracingDataModel.CurrentTracingTarget
                || m_ForceUpdate;

            m_CurrentFrame = m_Store.GetState().TracingDataModel.CurrentTracingFrame;
            m_CurrentStep = m_Store.GetState().TracingDataModel.CurrentTracingStep;
            m_CurrentTarget = m_Store.GetState().TracingDataModel.CurrentTracingTarget;

            return dirty;
        }
    }
}
