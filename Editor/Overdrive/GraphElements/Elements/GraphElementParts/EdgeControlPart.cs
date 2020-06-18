using System;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.GraphViewModel;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.GraphElements
{
    public class EdgeControlPart : BaseGraphElementPart
    {
        public static EdgeControlPart Create(string name, IGTFGraphElementModel model, IGraphElement ownerElement, string parentClassName)
        {
            if (model is IGTFEdgeModel)
            {
                return new EdgeControlPart(name, model, ownerElement, parentClassName);
            }

            return null;
        }

        protected EdgeControlPart(string name, IGTFGraphElementModel model, IGraphElement ownerElement, string parentClassName)
            : base(name, model, ownerElement, parentClassName) {}

        EdgeControl m_EdgeControl;

        public override VisualElement Root => m_EdgeControl;

        protected override void BuildPartUI(VisualElement container)
        {
            m_EdgeControl = new EdgeControl { name = PartName };
            m_EdgeControl.AddToClassList(m_ParentClassName.WithUssElement(PartName));

            m_EdgeControl.RegisterCallback<MouseEnterEvent>(OnMouseEnterEdge);
            m_EdgeControl.RegisterCallback<MouseLeaveEvent>(OnMouseLeaveEdge);

            container.Add(m_EdgeControl);
        }

        protected override void UpdatePartFromModel()
        {
            m_EdgeControl.RebuildControlPointsUI();

            if (m_Model is IGTFEdgeModel edgeModel)
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

            if (parent?.selected ?? false)
            {
                m_EdgeControl.ResetColor();
            }
            else
            {
                var edgeModel = m_Model as IGTFEdgeModel;
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
