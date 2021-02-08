using System;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public class GraphViewBlackboardWindow : GraphViewToolWindow
    {
        Blackboard m_Blackboard;

        const string k_ToolName = "Blackboard";

        protected override string ToolName => k_ToolName;

        protected override void OnEnable()
        {
            base.OnEnable();

            OnGraphViewChanged();
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            OnGraphViewChanging();
        }

        protected override void OnGraphViewChanging()
        {
            if (m_Blackboard != null)
            {
                rootVisualElement.Remove(m_Blackboard);
                m_Blackboard = null;
            }
        }

        protected override void OnGraphViewChanged()
        {
            if (m_SelectedGraphView != null)
            {
                m_Blackboard = m_SelectedGraphView.GetBlackboard();
                if (m_Blackboard != null)
                {
                    m_Blackboard.Windowed = true;
                    rootVisualElement.Add(m_Blackboard);
                }
            }
            else
            {
                m_Blackboard = null;
            }
        }

        protected override bool IsGraphViewSupported(GraphView gv)
        {
            return gv.SupportsWindowedBlackboard;
        }
    }
}
