using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public class EdgeControlPart : BaseModelUIPart
    {
        public static EdgeControlPart Create(string name, IGraphElementModel model, IModelUI ownerElement, string parentClassName)
        {
            if (model is IEdgeModel)
            {
                return new EdgeControlPart(name, model, ownerElement, parentClassName);
            }

            return null;
        }

        public override VisualElement Root => m_EdgeControl;

        EdgeControl m_EdgeControl;

        protected EdgeControlPart(string name, IGraphElementModel model, IModelUI ownerElement, string parentClassName)
            : base(name, model, ownerElement, parentClassName) {}

        protected override void BuildPartUI(VisualElement container)
        {
            m_EdgeControl = new EdgeControl() { name = PartName };
            m_EdgeControl.AddToClassList(m_ParentClassName.WithUssElement(PartName));

            m_EdgeControl.RegisterCallback<MouseEnterEvent>(OnMouseEnterEdge);
            m_EdgeControl.RegisterCallback<MouseLeaveEvent>(OnMouseLeaveEdge);

            container.Add(m_EdgeControl);
        }

        protected override void UpdatePartFromModel()
        {
            m_EdgeControl.RebuildControlPointsUI();

            if (m_Model is IEdgeModel edgeModel)
            {
                m_EdgeControl.OutputOrientation = edgeModel.FromPort?.Orientation ?? (edgeModel.ToPort?.Orientation ?? Orientation.Horizontal);
                m_EdgeControl.InputOrientation = edgeModel.ToPort?.Orientation ?? (edgeModel.FromPort?.Orientation ?? Orientation.Horizontal);
            }

            m_EdgeControl.UpdateLayout();
            UpdateEdgeControlColors();
            m_EdgeControl.MarkDirtyRepaint();
        }

        void UpdateEdgeControlColors()
        {
            var parent = m_OwnerElement as Edge;

            if (parent?.Selected ?? false)
            {
                m_EdgeControl.ResetColor();
            }
            else
            {
                var edgeModel = m_Model as IEdgeModel;
                var inputColor = Color.white;
                var outputColor = Color.white;

                if (edgeModel?.ToPort != null)
                    inputColor = edgeModel.ToPort.GetUI<Port>(m_OwnerElement.GraphView)?.PortColor ?? Color.white;
                else if (edgeModel?.FromPort != null)
                    inputColor = edgeModel.FromPort.GetUI<Port>(m_OwnerElement.GraphView)?.PortColor ?? Color.white;

                if (edgeModel?.FromPort != null)
                    outputColor = edgeModel.FromPort.GetUI<Port>(m_OwnerElement.GraphView)?.PortColor ?? Color.white;
                else if (edgeModel?.ToPort != null)
                    outputColor = edgeModel.ToPort.GetUI<Port>(m_OwnerElement.GraphView)?.PortColor ?? Color.white;

                if (parent?.IsGhostEdge ?? false)
                {
                    inputColor = new Color(inputColor.r, inputColor.g, inputColor.b, 0.5f);
                    outputColor = new Color(outputColor.r, outputColor.g, outputColor.b, 0.5f);
                }

                m_EdgeControl.SetColor(inputColor, outputColor);
            }
        }

        void OnMouseEnterEdge(MouseEnterEvent e)
        {
            if (e.target == m_EdgeControl)
            {
                m_EdgeControl.ResetColor();
            }
        }

        void OnMouseLeaveEdge(MouseLeaveEvent e)
        {
            if (e.target == m_EdgeControl)
            {
                UpdateEdgeControlColors();
            }
        }
    }
}
