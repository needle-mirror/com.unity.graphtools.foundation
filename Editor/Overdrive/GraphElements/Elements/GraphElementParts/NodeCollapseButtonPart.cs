using System;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using UnityEditor.GraphToolsFoundation.Overdrive.Bridge;

namespace UnityEditor.GraphToolsFoundation.Overdrive.GraphElements
{
    public class NodeCollapseButtonPart : CollapseButtonPart
    {
        public new static NodeCollapseButtonPart Create(string name, IGTFGraphElementModel model, IGraphElement graphElement, string parentClassName)
        {
            if (model is ICollapsible)
            {
                return new NodeCollapseButtonPart(name, model, graphElement, parentClassName);
            }

            return null;
        }

        protected NodeCollapseButtonPart(string name, IGTFGraphElementModel model, IGraphElement ownerElement, string parentClassName)
            : base(name, model, ownerElement, parentClassName) {}

        protected override void UpdatePartFromModel()
        {
            base.UpdatePartFromModel();

            if (CollapseButton != null)
            {
                if (m_Model is IHasPorts portHolder && portHolder.Ports != null)
                {
                    var allPortConnected = portHolder.Ports.All(port => port.IsConnected);
                    CollapseButton?.SetDisabledPseudoState(allPortConnected);
                }
            }
        }
    }
}
