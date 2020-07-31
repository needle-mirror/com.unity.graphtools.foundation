using System;
using UnityEditor.GraphToolsFoundation.Overdrive.GraphElements;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.GraphViewModel;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting
{
    public class EdgeBubblePart : UnityEditor.GraphToolsFoundation.Overdrive.GraphElements.EdgeBubblePart
    {
        public new static EdgeBubblePart Create(string name, IGTFGraphElementModel model, IGraphElement graphElement, string parentClassName)
        {
            if (model is EdgeModel)
            {
                return new EdgeBubblePart(name, model, graphElement, parentClassName);
            }

            return null;
        }

        protected EdgeBubblePart(string name, IGTFGraphElementModel model, IGraphElement ownerElement, string parentClassName)
            : base(name, model, ownerElement, parentClassName) {}

        protected override bool ShouldShow()
        {
            var edgeModel = m_Model as IGTFEdgeModel;
            var toPortNodeModel = edgeModel?.ToPort?.NodeModel;
            var fromPortNodeModel = edgeModel?.FromPort?.NodeModel;
            var portType = (edgeModel?.FromPort as PortModel)?.PortType ?? PortType.Data;

            return portType == PortType.Execution && (fromPortNodeModel != null || toPortNodeModel != null) &&
                !string.IsNullOrEmpty(edgeModel.EdgeLabel);
        }
    }
}
