using System;

namespace UnityEditor.GraphToolsFoundation.Overdrive.BasicModel
{
    /// <summary>
    /// Model for context nodes.
    /// </summary>
    [Serializable]
    public class ContextNodeModel : NodeModel, IContextNodeModel
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ContextNodeModel"/> class.
        /// </summary>
        public ContextNodeModel()
        {
            this.SetCapability(Overdrive.Capabilities.Collapsible, false);
        }
    }
}
