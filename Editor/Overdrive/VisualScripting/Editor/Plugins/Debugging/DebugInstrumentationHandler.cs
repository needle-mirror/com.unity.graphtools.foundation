using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
        CommandDispatcher m_CommandDispatcher;
        GraphView m_GraphView;
        List<TracingStep> GraphDebuggingData => m_CommandDispatcher.GraphToolState.TracingState.DebuggingData;
        PauseState m_PauseState = PauseState.Unpaused;
        PlayModeStateChange m_PlayState = PlayModeStateChange.EnteredEditMode;
        int? m_CurrentFrame;
        int? m_CurrentStep;
        int? m_CurrentTarget;
        Stopwatch m_Stopwatch;

        TracingToolbar m_TimelineToolbar;
        uint m_StateVersionCounter;

        public void Register(GraphViewEditorWindow window)
        {
            m_CommandDispatcher = window.CommandDispatcher;
            m_GraphView = window.GraphViews.First();
            EditorApplication.update += OnUpdate;
            EditorApplication.pauseStateChanged += OnEditorPauseStateChanged;
            EditorApplication.playModeStateChanged += OnEditorPlayModeStateChanged;
            m_CommandDispatcher.GraphToolState.GraphModel?.Stencil?.Debugger?.Start(m_CommandDispatcher.GraphToolState.GraphModel, m_CommandDispatcher.GraphToolState.TracingState.TracingEnabled);

            var root = window.rootVisualElement;
            if (m_TimelineToolbar == null)
            {
                m_TimelineToolbar = root.Q<TracingToolbar>();
                if (m_TimelineToolbar == null)
                {
                    m_TimelineToolbar = new TracingToolbar(m_GraphView, m_CommandDispatcher);
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
            m_CommandDispatcher.GraphToolState.GraphModel?.Stencil?.Debugger?.Stop();
            m_TimelineToolbar?.RemoveFromHierarchy();
        }

        public void OptionsMenu(GenericMenu menu)
        {
            menu.AddItem(new GUIContent("Dump Frame Trace"), false, DumpFrameTrace);
        }

        void DumpFrameTrace()
        {
            var state = m_CommandDispatcher.GraphToolState;
            var currentGraphModel = state?.GraphModel;
            var debugger = currentGraphModel?.Stencil?.Debugger;
            if (state == null || debugger == null)
                return;

            var tracingDataModel = state.TracingState;
            if (debugger.GetTracingSteps(currentGraphModel, tracingDataModel.CurrentTracingFrame,
                tracingDataModel.CurrentTracingTarget,
                out var stepList))
            {
                try
                {
                    var searcherItems = stepList.Select(MakeStepItem).ToList();
                    Searcher.SearcherWindow.Show(EditorWindow.focusedWindow, searcherItems, "Steps", item =>
                    {
                        if (item != null)
                            tracingDataModel.CurrentTracingStep = ((StepSearcherItem)item).Index;
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

        void OnUpdate()
        {
            if (m_TimelineToolbar == null)
                return;
            if (EditorApplication.isPlaying && !EditorApplication.isPaused)
            {
                m_CommandDispatcher.GraphToolState.TracingState.CurrentTracingFrame = Time.frameCount;
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

            var state = m_CommandDispatcher.GraphToolState;
            var currentGraphModel = state?.GraphModel;
            var debugger = currentGraphModel?.Stencil?.Debugger;
            if (state == null || debugger == null)
                return;

            var tracingDataModel = state.TracingState;
            if (needUpdate && debugger.GetTracingSteps(currentGraphModel, tracingDataModel.CurrentTracingFrame,
                tracingDataModel.CurrentTracingTarget,
                out var stepList))
            {
                tracingDataModel.MaxTracingStep = stepList == null ? 0 : stepList.Count;
                tracingDataModel.DebuggingData = stepList;
                HighlightTrace();
            }
        }

        void ClearHighlights()
        {
            var state = m_CommandDispatcher.GraphToolState;
            var currentGraphModel = state?.GraphModel;
            currentGraphModel?.DeleteBadgesOfType<DebuggingErrorBadgeModel>();
            currentGraphModel?.DeleteBadgesOfType<DebuggingValueBadgeModel>();

            foreach (Node x in m_GraphView.Nodes.ToList())
            {
                x.RemoveFromClassList(k_TraceHighlight);
                x.RemoveFromClassList(k_TraceSecondaryHighlight);
                x.RemoveFromClassList(k_ExceptionHighlight);

                if (x is Node n)
                {
                    n.Query<Port>().ForEach(p =>
                    {
                        p.ExecutionPortActive = false;
                    });
                }
            }
        }

        void HighlightTrace()
        {
            if (GraphDebuggingData != null)
            {
                var currentTracingStep = m_CommandDispatcher.GraphToolState.TracingState.CurrentTracingStep;
                if (currentTracingStep < 0 || currentTracingStep >= GraphDebuggingData.Count)
                {
                    m_CommandDispatcher.GraphToolState.TracingState.CurrentTracingStep = -1;
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
                m_GraphView.Edges.ForEach(e => e.MarkDirtyRepaint())).StartingIn(1);
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
                    step.NodeModel.GraphModel.AddBadge(new DebuggingValueBadgeModel(step));
                    break;
                case TracingStepType.ReadValue:
                    step.NodeModel.GraphModel.AddBadge(new DebuggingValueBadgeModel(step));
                    break;
                case TracingStepType.Error:
                    step.NodeModel.GraphModel.AddBadge(new DebuggingErrorBadgeModel(step));
                    break;
            }

            if (step.NodeModel?.HasProgress ?? false)
            {
                var node = step.NodeModel.GetUI<CollapsibleInOutNode>(m_GraphView);
                if (node != null)
                    node.Progress = step.Progress;
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
            var tracingDataModel = m_CommandDispatcher.GraphToolState.TracingState;

            bool dirty = m_CurrentFrame != tracingDataModel.CurrentTracingFrame
                || m_CurrentStep != tracingDataModel.CurrentTracingStep
                || m_CurrentTarget != tracingDataModel.CurrentTracingTarget
                || m_StateVersionCounter != m_CommandDispatcher.GraphToolState.Version;

            m_CurrentFrame = tracingDataModel.CurrentTracingFrame;
            m_CurrentStep = tracingDataModel.CurrentTracingStep;
            m_CurrentTarget = tracingDataModel.CurrentTracingTarget;
            m_StateVersionCounter = m_CommandDispatcher.GraphToolState.Version;

            return dirty;
        }
    }
}
