using System;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.GraphElements
{
    public class EdgeBubblePart : BaseGraphElementPart
    {
        public static EdgeBubblePart Create(string name, IGTFGraphElementModel model, IGraphElement graphElement, string parentClassName)
        {
            if (model is IGTFEdgeModel)
            {
                return new EdgeBubblePart(name, model, graphElement, parentClassName);
            }

            return null;
        }

        public static readonly string k_UssClassName = "ge-edge-bubble-part";

        protected EdgeBubblePart(string name, IGTFGraphElementModel model, IGraphElement ownerElement, string parentClassName)
            : base(name, model, ownerElement, parentClassName) {}

        protected EdgeBubble m_EdgeBubble;
        public override VisualElement Root => m_EdgeBubble;

        protected override void BuildPartUI(VisualElement container)
        {
            m_EdgeBubble = new EdgeBubble { name = PartName };
            m_EdgeBubble.AddToClassList(k_UssClassName);
            m_EdgeBubble.AddToClassList(m_ParentClassName.WithUssElement(PartName));
            container.Add(m_EdgeBubble);
        }

        protected override void PostBuildPartUI()
        {
            base.PostBuildPartUI();
            m_EdgeBubble.AddStylesheet("EdgeBubblePart.uss");
        }

        protected override void UpdatePartFromModel()
        {
            if (!(m_Model is IGTFEdgeModel edgeModel) || !(m_OwnerElement is VisualElement edge))
                return;

            if (ShouldShow())
            {
                VisualElement attachPoint = edge.Q<EdgeControl>() ?? edge;
                var offset = Vector2.zero;
                if (attachPoint is EdgeControl)
                {
                    offset = ComputePosition() - new Vector2(attachPoint.layout.xMin + attachPoint.layout.width / 2, attachPoint.layout.yMin + attachPoint.layout.height / 2);
                }

                m_EdgeBubble.SetAttacherOffset(offset);
                m_EdgeBubble.text = edgeModel.EdgeLabel;
                m_EdgeBubble.AttachTo(attachPoint, SpriteAlignment.Center);
                m_EdgeBubble.style.visibility = StyleKeyword.Null;
            }
            else
            {
                m_EdgeBubble.Detach();
                m_EdgeBubble.style.visibility = Visibility.Hidden;
            }
        }

        protected virtual bool ShouldShow()
        {
            var edgeModel = m_Model as IGTFEdgeModel;
            var toPortNodeModel = edgeModel?.ToPort?.NodeModel;
            var fromPortNodeModel = edgeModel?.FromPort?.NodeModel;

            return (fromPortNodeModel != null || toPortNodeModel != null) &&
                !string.IsNullOrEmpty(edgeModel.EdgeLabel);
        }

        Vector2 ComputePosition()
        {
            var edge = m_OwnerElement as Edge;
            var edgeControl = edge?.Q<EdgeControl>();

            if (edgeControl == null)
                return Vector2.zero;

            if (edgeControl.RenderPoints.Count > 0)
            {
                const int intersectionSquaredRadius = 10000;

                // Find the segment that intersect a circle of radius sqrt(targetSqDistance) centered at `from`.
                float targetSqDistance = Mathf.Min(intersectionSquaredRadius, (edge.To - edge.From).sqrMagnitude / 4);
                var localFrom = edge.ChangeCoordinatesTo(edgeControl, edge.From);
                for (var index = 0; index < edgeControl.RenderPoints.Count; index++)
                {
                    var point = edgeControl.RenderPoints[index];
                    if ((point - localFrom).sqrMagnitude >= targetSqDistance)
                    {
                        return edgeControl.ChangeCoordinatesTo(edge, edgeControl.RenderPoints[index]);
                    }
                }
            }

            return edgeControl.ChangeCoordinatesTo(edge, Vector2.zero);
        }
    }
}
