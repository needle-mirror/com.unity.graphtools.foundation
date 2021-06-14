using System;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Base class for toolbars in GraphTools Foundation.
    /// </summary>
    public class Toolbar : UIElements.Toolbar
    {
        public new static readonly string ussClassName = "ge-toolbar";

        protected readonly CommandDispatcher m_CommandDispatcher;
        protected readonly GraphView m_GraphView;

        public Toolbar(CommandDispatcher commandDispatcher, GraphView graphView)
        {
            AddToClassList(ussClassName);
            this.AddStylesheet("Toolbar.uss");

            m_CommandDispatcher = commandDispatcher;
            m_GraphView = graphView;
        }
    }
}
