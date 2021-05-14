using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.MathBook
{
    class SimpleGraphView : GraphView
    {
        static readonly Vector2 s_CopyOffset = new Vector2(50, 50);
        SimpleGraphViewWindow m_SimpleGraphViewWindow;

        public SimpleGraphViewWindow window => m_SimpleGraphViewWindow;

        public SimpleGraphView(SimpleGraphViewWindow simpleGraphViewWindow, CommandDispatcher store, string graphViewName)
            : base(simpleGraphViewWindow, store, graphViewName)
        {
            m_SimpleGraphViewWindow = simpleGraphViewWindow;
        }
    }
}
