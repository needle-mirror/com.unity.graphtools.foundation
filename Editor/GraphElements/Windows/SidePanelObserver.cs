using System.Linq;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    class SidePanelObserver : StateObserver<GraphToolState>
    {
        GraphViewEditorWindow m_Window;

        public SidePanelObserver(GraphViewEditorWindow window)
            : base(new[] { nameof(GraphToolState.SelectionState) },
                new[] { nameof(GraphToolState.ModelInspectorState) })
        {
            m_Window = window;
        }

        protected override void Observe(GraphToolState state)
        {
            using (var observation = this.ObserveState(state.SelectionState))
            {
                if (observation.UpdateType != UpdateType.None)
                {
                    var graphModel = state.GraphViewState.GraphModel;
                    var lastSelectedNode = state.SelectionState.GetSelection(graphModel).OfType<INodeModel>().LastOrDefault();

                    using (var updater = state.ModelInspectorState.UpdateScope)
                    {
                        updater.SetModel(lastSelectedNode);
                    }
                }
            }
        }
    }
}
