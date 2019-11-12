using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model;
using UnityEngine;
using UnityEngine.UIElements;
using CompilationOptions = UnityEngine.VisualScripting.CompilationOptions;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

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
        List<State.DebuggingDataModel> GraphDebuggingData => m_Store.GetState()?.DebuggingData;
        PauseState m_PauseState = PauseState.Unpaused;
        PlayModeStateChange m_PlayState = PlayModeStateChange.EnteredEditMode;
        int? m_CurrentFrame;
        int? m_CurrentStep;
        int? m_CurrentTarget;
        Stopwatch m_Stopwatch;
        bool m_ForceUpdate;
        public const string RecorderVariableName = "recorder";

        public void Register(Store store, GraphView graphView)
        {
            m_Store = store;
            m_GraphView = graphView;
            EditorApplication.update += OnUpdate;
            EditorApplication.pauseStateChanged += OnEditorPauseStateChanged;
            EditorApplication.playModeStateChanged += OnEditorPlayModeStateChanged;
            m_Store.StateChanged += OnStateChangeUpdate;
        }

        public void Unregister()
        {
            ClearHighlights();
            // ReSharper disable once DelegateSubtraction
            EditorApplication.update -= OnUpdate;
            EditorApplication.pauseStateChanged -= OnEditorPauseStateChanged;
            EditorApplication.playModeStateChanged -= OnEditorPlayModeStateChanged;
            m_Store.StateChanged -= OnStateChangeUpdate;
        }

        public virtual void OptionsMenu(GenericMenu menu)
        {
            MenuItem("Tracing/Clear", false, () => DebuggerTracer.LoadData(null));

            MenuItem("Tracing/Load", false, () =>
            {
                if (!GetAssetAndTracePath(out var assetModel, out var _, out var traceName))
                    return;
                TraceDump traceDump;
                using (var traceStream = File.OpenRead(traceName))
                    traceDump = TraceDump.Deserialize(new BinaryReader(traceStream));
                var graphTrace = new DebuggerTracer.GraphTrace(traceDump.FrameData);
                DebuggerTracer.GraphReference graphReference = DebuggerTracer.GraphReference.FromId(assetModel.GetInstanceID());
                DebuggerTracer.LoadData(new Dictionary<DebuggerTracer.GraphReference, DebuggerTracer.GraphTrace>
                    {[graphReference] = graphTrace});
            });
            MenuItem("Tracing/Save", false, () =>
            {
                if (!GetAssetAndTracePath(out var assetModel, out var path, out var traceName))
                    return;
                var gd = DebuggerTracer.GetGraphData(assetModel.GetInstanceID(), false);
                if (gd != null)
                {
                    TraceDump dump = new TraceDump(path, gd.AllFrames.ToArray());
                    using (var s = File.OpenWrite(traceName))
                    {
                        BinaryWriter w = new BinaryWriter(s);
                        dump.Serialize(w);
                        w.Flush();
                    }
                }
            });

            bool GetAssetAndTracePath(out Object assetModel, out string path, out string traceName)
            {
                assetModel = m_Store.GetState().CurrentGraphModel.AssetModel as Object;
                if (!assetModel)
                {
                    path = null;
                    traceName = null;
                    return false;
                }

                path = AssetDatabase.GetAssetPath(assetModel);
                traceName = Path.GetFileNameWithoutExtension(path) + ".bin";
                return true;
            }

            void MenuItem(string title, bool value, GenericMenu.MenuFunction onToggle)
                => menu.AddItem(VseUtility.CreatTextContent(title), value, onToggle);
        }

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

            var currentGraphModel = m_Store.GetState()?.CurrentGraphModel;
            var graph = (Object)currentGraphModel?.AssetModel;
            if (graph == null)
                return;
            var trace = DebuggerTracer.GetGraphData(graph.GetInstanceID(), false) ?
                .GetFrameData(m_Store.GetState().currentTracingFrame, false) ?
                    .GetExistingEntityFrameTrace(m_Store.GetState().currentTracingTarget);

            if (needUpdate && trace != null && trace.steps.Count > 0)
            {
                m_Store.GetState().maxTracingStep = trace.steps.Count - 1;
                m_Store.GetState().DebuggingData = trace.steps.Select((x, i) =>
                {
                    var nodeModel = Get(x.nodeId1, x.nodeId2);
                    Dictionary<INodeModel, string> valueRecords = null;
                    if (trace.values != null && trace.values.TryGetValue(i, out var values))
                    {
                        valueRecords = values
                            .Select(r => (Model: Get(r.nodeId1, r.nodeId2), r.readableValue))
                            .Where(t => t.Model != null)
                            .ToDictionary(t => t.Model, t => t.readableValue);
                    }

                    return new State.DebuggingDataModel
                    {
                        nodeModel = nodeModel, type = x.type, text = x.exceptionText, values = valueRecords,
                        progress = x.progress,
                    };
                }).ToList();
                HighlightTrace();
            }

            INodeModel Get(ulong nodeId1, ulong nodeId2)
            {
                var guid = SerializableGUID.FromParts(nodeId1, nodeId2);
                return currentGraphModel != null && currentGraphModel.NodesByGuid.TryGetValue(guid, out var n) ? n : null;
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
                    VseUIController.ClearValue(x);

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
                var currentTracingStep = m_Store.GetState().currentTracingStep;
                if (currentTracingStep < 0 || currentTracingStep >= GraphDebuggingData.Count)
                {
                    m_Store.GetState().currentTracingStep = -1;
                    foreach (var step in GraphDebuggingData)
                    {
                        AddStyleClassToModel(step, modelsToNodeUiMapping, k_TraceHighlight);
                        DisplayStepValues(step, modelsToNodeUiMapping);
                    }
                }
                else
                {
                    var step = GraphDebuggingData[currentTracingStep];
                    AddStyleClassToModel(step, modelsToNodeUiMapping, k_TraceSecondaryHighlight);
                    DisplayStepValues(step, modelsToNodeUiMapping);

                    for (var i = 0; i < currentTracingStep; i++)
                    {
                        step = GraphDebuggingData[i];
                        AddStyleClassToModel(step, modelsToNodeUiMapping, k_TraceHighlight);
                        DisplayStepValues(step, modelsToNodeUiMapping);
                    }
                }
            }
        }

        void DisplayStepValues(State.DebuggingDataModel step, Dictionary<IGraphElementModel, GraphElement> modelsToNodeUiMapping)
        {
            if (step.values != null)
                foreach (var value in step.values)
                    AddValueToNode(modelsToNodeUiMapping, value.Key, value.Value);

            if (step.nodeModel is INodeModelProgress && modelsToNodeUiMapping.TryGetValue(step.nodeModel, out var node) && node is Node vsNode)
            {
                vsNode.Progress = step.progress;
            }
        }

        void AddValueToNode(IReadOnlyDictionary<IGraphElementModel, GraphElement> modelsToNodeUiMapping, INodeModel node, string valueReadableValue)
        {
            if (node != null && modelsToNodeUiMapping.TryGetValue(node, out GraphElement ui))
            {
                if (m_PauseState == PauseState.Paused || m_PlayState == PlayModeStateChange.EnteredEditMode)
                {
                    var n = (Experimental.GraphView.Node)ui;
                    Port p = n.outputContainer.childCount > 0
                        ? n.outputContainer[0] as Port
                        : null;
                    IBadgeContainer badgeContainer = (IBadgeContainer)n;
                    if (p == null)
                        return;
                    VisualElement cap = p.Q(className: "connectorCap");
                    ((VseGraphView)m_GraphView).UIController.AttachValue(badgeContainer, cap, valueReadableValue, p.portColor, SpriteAlignment.BottomRight);
                }
            }
        }

        void AddStyleClassToModel(State.DebuggingDataModel step, IReadOnlyDictionary<IGraphElementModel, GraphElement> modelsToNodeUiMapping, string highlightStyle)
        {
            if (step.nodeModel != null && modelsToNodeUiMapping.TryGetValue(step.nodeModel, out GraphElement ui))
            {
                if (step.type == DebuggerTracer.EntityFrameTrace.StepType.Exception)
                {
                    ui.AddToClassList(k_ExceptionHighlight);

                    if (m_PauseState == PauseState.Paused || m_PlayState == PlayModeStateChange.EnteredEditMode)
                    {
                        ((VseGraphView)m_GraphView).UIController.AttachErrorBadge(ui, step.text, SpriteAlignment.TopLeft);
                    }
                }
                else
                {
                    ui.AddToClassList(highlightStyle);
                }
            }
        }

        bool IsDirty()
        {
            bool dirty = m_CurrentFrame != m_Store.GetState().currentTracingFrame
                || m_CurrentStep != m_Store.GetState().currentTracingStep
                || m_CurrentTarget != m_Store.GetState().currentTracingTarget
                || m_ForceUpdate;

            m_CurrentFrame = m_Store.GetState().currentTracingFrame;
            m_CurrentStep = m_Store.GetState().currentTracingStep;
            m_CurrentTarget = m_Store.GetState().currentTracingTarget;

            return dirty;
        }
    }
}
