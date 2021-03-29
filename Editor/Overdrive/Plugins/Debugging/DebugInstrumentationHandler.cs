using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEditor.Searcher;
using UnityEngine;
using UnityEngine.UIElements;
using Debug = UnityEngine.Debug;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Plugins.Debugging
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
        PlayModeStateChange m_PlayState = PlayModeStateChange.EnteredEditMode;
        Stopwatch m_Stopwatch;

        TracingToolbar m_TimelineToolbar;
        uint m_StateVersionCounter;
        DebugDataObserver m_DebugDataObserver;

        public void Register(GraphViewEditorWindow window)
        {
            m_CommandDispatcher = window.CommandDispatcher;
            m_GraphView = window.GraphViews.First();

            m_DebugDataObserver = new DebugDataObserver(this);
            m_CommandDispatcher.RegisterObserver(m_DebugDataObserver);

            EditorApplication.update += OnUpdate;
            EditorApplication.pauseStateChanged += OnEditorPauseStateChanged;
            EditorApplication.playModeStateChanged += OnEditorPlayModeStateChanged;
            m_CommandDispatcher.GraphToolState.WindowState.GraphModel?.Stencil?.Debugger?.Start(m_CommandDispatcher.GraphToolState.WindowState.GraphModel, m_CommandDispatcher.GraphToolState.TracingControlState.TracingEnabled);

            var root = window.rootVisualElement;
            if (m_TimelineToolbar == null)
            {
                m_TimelineToolbar = root.SafeQ<TracingToolbar>();
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
            m_CommandDispatcher.UnregisterObserver(m_DebugDataObserver);

            ClearHighlights(m_CommandDispatcher.GraphToolState);
            // ReSharper disable once DelegateSubtraction
            EditorApplication.update -= OnUpdate;
            EditorApplication.pauseStateChanged -= OnEditorPauseStateChanged;
            EditorApplication.playModeStateChanged -= OnEditorPlayModeStateChanged;
            m_CommandDispatcher.GraphToolState.WindowState.GraphModel?.Stencil?.Debugger?.Stop();
            m_TimelineToolbar?.RemoveFromHierarchy();
        }

        public void OptionsMenu(GenericMenu menu)
        {
            menu.AddItem(new GUIContent("Dump Frame Trace"), false, DumpFrameTrace);
        }

        // PF FIXME to command
        void DumpFrameTrace()
        {
            var state = m_CommandDispatcher.GraphToolState;
            var currentGraphModel = state?.WindowState.GraphModel;
            var debugger = currentGraphModel?.Stencil?.Debugger;
            if (state == null || debugger == null)
                return;

            var tracingDataModel = state.TracingControlState;
            if (debugger.GetTracingSteps(currentGraphModel, tracingDataModel.CurrentTracingFrame,
                tracingDataModel.CurrentTracingTarget,
                out var stepList))
            {
                try
                {
                    var searcherItems = stepList.Select(MakeStepItem).ToList();
                    Searcher.SearcherWindow.Show(EditorWindow.focusedWindow, searcherItems, "Steps", item =>
                    {
                        using (var updater = tracingDataModel.UpdateScope)
                        {
                            if (item != null)
                                updater.CurrentTracingStep = ((StepSearcherItem)item).Index;
                        }

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
            public readonly int Index;

            public StepSearcherItem(TracingStep step, int i) : base(GetName(step), "No help available.")
            {
                Index = i;
            }

            static string GetName(TracingStep step)
            {
                return $"{step.Type} {step.NodeModel} {step.PortModel}";
            }
        }

        // PF FIXME Register plugin instead of using editor update.
        void OnUpdate()
        {
            if (m_TimelineToolbar == null)
                return;

            var graphToolState = m_CommandDispatcher.GraphToolState;

            using (var updater = graphToolState.TracingControlState.UpdateScope)
            {
                if (EditorApplication.isPlaying && !EditorApplication.isPaused)
                {
                    updater.CurrentTracingFrame = Time.frameCount;
                }

                m_TimelineToolbar.UpdateTracingMenu(updater);
            }

            m_TimelineToolbar?.SyncVisible();

        }

        void OnEditorPauseStateChanged(PauseState state)
        {
            // TODO Save tracing data
        }

        void OnEditorPlayModeStateChanged(PlayModeStateChange state)
        {
            m_PlayState = state;

            if (m_PlayState == PlayModeStateChange.ExitingPlayMode)
            {
                ClearHighlights(m_CommandDispatcher.GraphToolState);
                m_Stopwatch?.Stop();
                m_Stopwatch = null;
            }
        }

        internal void MapDebuggingData(GraphToolState state)
        {
            bool needUpdate = false;
            if (m_Stopwatch == null)
                m_Stopwatch = Stopwatch.StartNew();
            else if (EditorApplication.isPaused || m_Stopwatch.ElapsedMilliseconds > k_UpdateIntervalMs)
            {
                needUpdate = true;
                m_Stopwatch.Restart();
            }

            var currentGraphModel = state?.GraphViewState.GraphModel;
            var debugger = currentGraphModel?.Stencil?.Debugger;
            if (state == null || debugger == null)
                return;

            if (needUpdate && debugger.GetTracingSteps(currentGraphModel, state.TracingControlState.CurrentTracingFrame,
                state.TracingControlState.CurrentTracingTarget,
                out var stepList))
            {
                using (var updater = state.TracingDataState.UpdateScope)
                {
                    updater.MaxTracingStep = stepList?.Count ?? 0;
                    updater.DebuggingData = stepList;
                }

                // PF FIXME HighlightTrace should be an observer on tracing states.
                ClearHighlights(state);
                HighlightTrace(state.TracingControlState.CurrentTracingStep, state.TracingDataState.DebuggingData);
            }
        }

        void ClearHighlights(GraphToolState state)
        {
            var currentGraphModel = state?.WindowState.GraphModel;
            currentGraphModel?.DeleteBadgesOfType<DebuggingErrorBadgeModel>();
            currentGraphModel?.DeleteBadgesOfType<DebuggingValueBadgeModel>();

            foreach (Node x in m_GraphView.Nodes.ToList())
            {
                x.RemoveFromClassList(k_TraceHighlight);
                x.RemoveFromClassList(k_TraceSecondaryHighlight);
                x.RemoveFromClassList(k_ExceptionHighlight);

                if (x is Node n)
                {
                    n.Query<DebuggingPort>().ForEach(p =>
                    {
                        p.ExecutionPortActive = false;
                    });
                }
            }
        }

        void HighlightTrace(int currentTracingStep, IReadOnlyList<TracingStep> graphDebuggingData)
        {
            if (graphDebuggingData != null)
            {
                if (currentTracingStep < 0 || currentTracingStep >= graphDebuggingData.Count)
                {
                    foreach (TracingStep step in graphDebuggingData)
                    {
                        AddStyleClassToModel(step, k_TraceHighlight);
                        DisplayStepValues(step);
                    }
                }
                else
                {
                    for (var i = 0; i < currentTracingStep; i++)
                    {
                        var step = graphDebuggingData[i];
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
                    var p = step.PortModel.GetUI<DebuggingPort>(m_GraphView);
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
    }
}
