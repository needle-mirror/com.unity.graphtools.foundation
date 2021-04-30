using UnityEngine;
namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.Contexts.UI
{
    class ContextGraphView : GraphView
    {
        ContextGraphViewWindow m_SimpleGraphViewWindow;

        public ContextGraphViewWindow window
        {
            get { return m_SimpleGraphViewWindow; }
        }

        public ContextGraphView(ContextGraphViewWindow simpleGraphViewWindow, bool withWindowedTools, CommandDispatcher store) : base(simpleGraphViewWindow, store, "SimpleGraphView")
        {
            m_SimpleGraphViewWindow = simpleGraphViewWindow;
        }
    }
}
