using System;

namespace UnityEditor.GraphToolsFoundation.Overdrive.BasicModel
{
    [Serializable]
    public class ContextNodeModel : NodeModel, IContextNodeModel
    {
        public ContextNodeModel()
        {
            InternalInitCapabilities();
        }

        protected override void InitCapabilities()
        {
            base.InitCapabilities();

            InternalInitCapabilities();
        }

        void InternalInitCapabilities()
        {
            this.SetCapability(Overdrive.Capabilities.Collapsible, false);
        }
    }
}
