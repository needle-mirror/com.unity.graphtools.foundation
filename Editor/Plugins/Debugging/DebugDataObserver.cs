using UnityEngine.GraphToolsFoundation.CommandStateObserver;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Plugins.Debugging
{
    /// <summary>
    /// An observer that updates debug data.
    /// </summary>
    class DebugDataObserver : StateObserver<GraphToolState>
    {
        DebugInstrumentationHandler m_DebugInstrumentationHandler;

        /// <summary>
        /// Initializes a new instance of the <see cref="DebugDataObserver" /> class.
        /// </summary>
        public DebugDataObserver(DebugInstrumentationHandler handler)
            : base(new[]
                {
                    nameof(GraphToolState.GraphViewState),
                    nameof(GraphToolState.TracingControlState)
                },
                new[]
                {
                    nameof(GraphToolState.TracingDataState)
                })
        {
            m_DebugInstrumentationHandler = handler;
        }

        /// <inheritdoc/>
        protected override void Observe(GraphToolState state)
        {
            using (var gvObservation = this.ObserveState(state.GraphViewState))
            using (var tsObservation = this.ObserveState(state.TracingControlState))
            {
                var updateType = gvObservation.UpdateType.Combine(tsObservation.UpdateType);

                if (updateType != UpdateType.None)
                    m_DebugInstrumentationHandler.MapDebuggingData(state);
            }
        }
    }
}
