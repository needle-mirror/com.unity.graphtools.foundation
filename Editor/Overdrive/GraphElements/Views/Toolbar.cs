using System;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public class Toolbar : UIElements.Toolbar
    {
        protected readonly Store m_Store;
        protected readonly GraphView m_GraphView;

        public Toolbar(Store store, GraphView graphView)
        {
            AddToClassList("gtf-toolbar");
            this.AddStylesheet("Toolbar.uss");

            m_Store = store;
            m_GraphView = graphView;
        }
    }
}
