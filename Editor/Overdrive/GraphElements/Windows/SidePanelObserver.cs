using System.Linq;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    class SidePanelObserver : StateObserver
    {
        GraphViewEditorWindow m_Window;

        public SidePanelObserver(GraphViewEditorWindow window)
            : base(nameof(GraphToolState.SelectionState))
        {
            m_Window = window;
        }

        public override void Observe(GraphToolState state)
        {
            using (var observation = this.ObserveState(state.SelectionState))
            {
                if (observation.UpdateType != UpdateType.None)
                {
                    var graphModel = state.GraphViewState.GraphModel;
                    var lastSelectedNode = state.SelectionState.GetSelection(graphModel).OfType<INodeModel>().LastOrDefault();
                    if (lastSelectedNode == null)
                    {
                        m_Window.ClearNodeInSidePanel();
                    }
                    else
                    {
                        m_Window.ShowNodeInSidePanel(lastSelectedNode, true);
                    }
                }
            }
        }
    }
}
