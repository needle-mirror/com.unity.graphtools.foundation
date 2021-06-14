using System.Diagnostics;
using System.Linq;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// An observer that automatically processes the graph when it is updated.
    /// </summary>
    /// <remarks>If the preference <see cref="BoolPref.AutoProcess"/> is true,
    /// the graph will only be processed after the mouse stays idle for
    /// <see cref="k_IdleTimeBeforeGraphProcessingMs"/> in edit mode or for
    /// <see cref="k_IdleTimeBeforeGraphProcessingMsPlayMode"/> in play mode.
    /// </remarks>
    public class AutomaticGraphProcessor : StateObserver<GraphToolState>
    {
        const int k_IdleTimeBeforeGraphProcessingMs = 1000;
        const int k_IdleTimeBeforeGraphProcessingMsPlayMode = 1000;

        readonly Stopwatch m_IdleTimer;
        bool m_LastObservedAutoProcessPref;
        PluginRepository m_PluginRepository;
        GraphToolState m_State;

        /// <summary>
        /// Initializes a new instance of the <see cref="AutomaticGraphProcessor" /> class.
        /// </summary>
        /// <param name="pluginRepository">The plugin repository.</param>
        public AutomaticGraphProcessor(PluginRepository pluginRepository)
            : base(new[]
                {
                    nameof(GraphToolState.GraphViewState),
                    nameof(GraphToolState.TracingStatusState)
                },
                new[]
                {
                    nameof(GraphToolState.GraphProcessingState)
                })
        {
            m_IdleTimer = new Stopwatch();

            m_PluginRepository = pluginRepository;
        }

        /// <inheritdoc/>
        protected override void Observe(GraphToolState state)
        {
            m_State = state;

            if (state.Preferences.GetBool(BoolPref.AutoProcess))
            {
                if (!m_IdleTimer.IsRunning)
                {
                    ResetTimer();
                }

                if (!m_LastObservedAutoProcessPref)
                    EditorApplication.update += OnUpdate;

                m_LastObservedAutoProcessPref = true;
            }
            else
            {
                if (m_IdleTimer.IsRunning)
                {
                    StopTimer();
                }

                if (m_LastObservedAutoProcessPref)
                    EditorApplication.update -= OnUpdate;

                // Force update if auto-process was just switched off.
                ObserveNow(state, m_LastObservedAutoProcessPref);
                m_LastObservedAutoProcessPref = false;
            }
        }

        void ObserveIfIdle(GraphToolState state)
        {
            var elapsedTime = m_IdleTimer.ElapsedMilliseconds;
            if (elapsedTime >= (EditorApplication.isPlaying
                ? k_IdleTimeBeforeGraphProcessingMsPlayMode
                : k_IdleTimeBeforeGraphProcessingMs))
            {
                ResetTimer();
                ObserveNow(state, false);
            }
            else
            {
                // We only want to display a notification that we will process the graph.
                // We need to check if the state components were modified, but
                // without updating our internal version numbers (they will be
                // updated when we actually process the graph). We use PeekAtState.
                using (var gvObservation = this.PeekAtState(state.GraphViewState))
                using (var tsObservation = this.PeekAtState(state.TracingStatusState))
                {
                    var gvUpdateType = gvObservation.UpdateType;
                    if (gvUpdateType == UpdateType.Partial)
                    {
                        // Adjust gvUpdateType if there was no modifications on the graph model
                        var changeset = state.GraphViewState.GetAggregatedChangeset(gvObservation.LastObservedVersion);
                        if (!changeset.NewModels.Any() && !changeset.ChangedModels.Any() && !changeset.DeletedModels.Any())
                            gvUpdateType = UpdateType.None;
                    }

                    var shouldRebuild = gvUpdateType.Combine(tsObservation.UpdateType) != UpdateType.None;
                    if (state.GraphProcessingState.GraphProcessingPending != shouldRebuild)
                    {
                        using (var updater = state.GraphProcessingState.UpdateScope)
                        {
                            updater.GraphProcessingPending = shouldRebuild;
                        }
                    }
                }
            }
        }

        void ObserveNow(GraphToolState state, bool forceUpdate)
        {
            using (var gvObservation = this.ObserveState(state.GraphViewState))
            using (var tsObservation = this.ObserveState(state.TracingStatusState))
            {
                var gvUpdateType = gvObservation.UpdateType;
                if (gvUpdateType == UpdateType.Partial)
                {
                    // Adjust gvUpdateType if there was no modifications on the graph model
                    var changeset = state.GraphViewState.GetAggregatedChangeset(gvObservation.LastObservedVersion);
                    if (!changeset.NewModels.Any() && !changeset.ChangedModels.Any() && !changeset.DeletedModels.Any())
                        gvUpdateType = UpdateType.None;
                }

                if (forceUpdate || gvUpdateType.Combine(tsObservation.UpdateType) != UpdateType.None)
                {
                    var results = state.GraphViewState.GraphModel.ProcessGraph(m_PluginRepository,
                        RequestGraphProcessingOptions.Default, state.TracingStatusState.TracingEnabled);

                    if (results != null || state.GraphProcessingState.GraphProcessingPending)
                    {
                        using (var updater = state.GraphProcessingState.UpdateScope)
                        {
                            updater.GraphProcessingPending = false;

                            if (results != null)
                                updater.SetResults(results,
                                    GraphProcessingHelper.GetErrors((Stencil)state.GraphViewState.GraphModel.Stencil, results));
                        }
                    }
                }
            }
        }

        void OnUpdate()
        {
            ObserveIfIdle(m_State);
        }

        /// <summary>
        /// Resets the idle timer.
        /// </summary>
        public void ResetTimer()
        {
            m_IdleTimer.Restart();
        }

        /// <summary>
        /// Stops the idle timer.
        /// </summary>
        public void StopTimer()
        {
            m_IdleTimer.Stop();
        }
    }
}
