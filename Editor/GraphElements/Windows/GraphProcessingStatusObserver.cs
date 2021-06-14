using UnityEngine.GraphToolsFoundation.CommandStateObserver;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    class GraphProcessingStatusObserver : StateObserver<GraphToolState>
    {
        Label m_StatusLabel;
        ErrorToolbar m_ErrorToolbar;

        public GraphProcessingStatusObserver(Label statusLabel, ErrorToolbar errorToolbar)
            : base(nameof(GraphToolState.GraphProcessingState))
        {
            m_StatusLabel = statusLabel;
            m_ErrorToolbar = errorToolbar;
        }

        protected override void Observe(GraphToolState state)
        {
            using (var observation = this.ObserveState(state.GraphProcessingState))
            {
                if (observation.UpdateType != UpdateType.None)
                {
                    if (m_ErrorToolbar?.panel != null)
                        m_ErrorToolbar?.UpdateUI();

                    m_StatusLabel?.EnableInClassList(
                        GraphViewEditorWindow.graphProcessingPendingUssClassName,
                        state.GraphProcessingState.GraphProcessingPending);
                }
            }
        }
    }
}
