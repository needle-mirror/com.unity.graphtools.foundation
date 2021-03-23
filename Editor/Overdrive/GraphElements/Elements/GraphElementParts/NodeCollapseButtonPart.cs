using System;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive.Bridge;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public class NodeCollapseButtonPart : CollapseButtonPart
    {
        public new static NodeCollapseButtonPart Create(string name, IGraphElementModel model, IModelUI modelUI, string parentClassName)
        {
            if (model is ICollapsible)
            {
                return new NodeCollapseButtonPart(name, model, modelUI, parentClassName);
            }

            return null;
        }

        protected NodeCollapseButtonPart(string name, IGraphElementModel model, IModelUI ownerElement, string parentClassName)
            : base(name, model, ownerElement, parentClassName) { }

        protected override void UpdatePartFromModel()
        {
            base.UpdatePartFromModel();

            if (CollapseButton != null)
            {
                if (m_Model is IPortNodeModel portHolder && portHolder.Ports != null)
                {
                    var allPortConnected = portHolder.Ports.All(port => port.IsConnected());
                    CollapseButton?.SetDisabledPseudoState(allPortConnected);
                }
            }
        }
    }
}
