namespace UnityEditor.GraphToolsFoundation.Overdrive.GraphElements
{
    public class GraphViewBlackboardWindow : GraphViewToolWindow
    {
        Blackboard m_Blackboard;

        const string k_ToolName = "Blackboard";

        protected override string ToolName => k_ToolName;

        new void OnEnable()
        {
            base.OnEnable();

            OnGraphViewChanged();
        }

        void OnDisable()
        {
            if (m_SelectedGraphView != null && m_Blackboard != null)
            {
                m_SelectedGraphView.ReleaseBlackboard(m_Blackboard);
            }
        }

        protected override void OnGraphViewChanging()
        {
            if (m_Blackboard != null)
            {
                if (m_SelectedGraphView != null)
                {
                    m_SelectedGraphView.ReleaseBlackboard(m_Blackboard);
                }
                rootVisualElement.Remove(m_Blackboard);
                m_Blackboard = null;
            }
        }

        protected override void OnGraphViewChanged()
        {
            if (m_SelectedGraphView != null)
            {
                m_Blackboard = m_SelectedGraphView.GetBlackboard();
                m_Blackboard.windowed = true;
                rootVisualElement.Add(m_Blackboard);
            }
            else
            {
                m_Blackboard = null;
            }
        }

        protected override bool IsGraphViewSupported(GraphView gv)
        {
            return gv.supportsWindowedBlackboard;
        }
    }
}
