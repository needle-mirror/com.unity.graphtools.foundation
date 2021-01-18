using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public class PortConnectorPart : BaseGraphElementPart
    {
        public static readonly string ussClassName = "ge-port-connector-part";
        public static readonly string connectorUssName = "connector";
        public static readonly string connectorCapUssName = "cap";
        public static readonly string labelName = "label";

        public static PortConnectorPart Create(string name, IGraphElementModel model, IGraphElement graphElement, string parentClassName)
        {
            if (model is IPortModel)
            {
                return new PortConnectorPart(name, model, graphElement, parentClassName);
            }

            return null;
        }

        Label m_ConnectorLabel;

        VisualElement m_ConnectorBox;

        VisualElement m_ConnectorBoxCap;

        VisualElement m_Root;

        bool m_Hovering;

        public override VisualElement Root => m_Root;

        protected PortConnectorPart(string name, IGraphElementModel model, IGraphElement ownerElement, string parentClassName)
            : base(name, model, ownerElement, parentClassName) {}

        protected override void BuildPartUI(VisualElement container)
        {
            m_Root = new VisualElement { name = PartName };
            m_Root.AddToClassList(ussClassName);
            m_Root.AddToClassList(m_ParentClassName.WithUssElement(PartName));

            m_ConnectorBox = new VisualElement { name = connectorUssName };
            m_ConnectorBox.AddToClassList(ussClassName.WithUssElement(connectorUssName));
            m_ConnectorBox.AddToClassList(m_ParentClassName.WithUssElement(connectorUssName));
            m_Root.Add(m_ConnectorBox);

            m_ConnectorBoxCap = new VisualElement { name = connectorCapUssName };
            m_ConnectorBoxCap.AddToClassList(ussClassName.WithUssElement(connectorCapUssName));
            m_ConnectorBoxCap.AddToClassList(m_ParentClassName.WithUssElement(connectorCapUssName));
            m_ConnectorBox.Add(m_ConnectorBoxCap);

            if (m_Model is IHasTitle)
            {
                m_ConnectorLabel = new Label { name = labelName };
                m_ConnectorLabel.AddToClassList(ussClassName.WithUssElement(labelName));
                m_ConnectorLabel.AddToClassList(m_ParentClassName.WithUssElement(labelName));
                m_Root.Add(m_ConnectorLabel);
            }

            if (m_ConnectorBox != null)
            {
                m_ConnectorBox.RegisterCallback<MouseEnterEvent>(OnMouseEnter);
                m_ConnectorBox.RegisterCallback<MouseLeaveEvent>(OnMouseLeave);
            }

            container.Add(m_Root);
        }

        protected override void PostBuildPartUI()
        {
            base.PostBuildPartUI();
            m_Root.AddStylesheet("PortConnectorPart.uss");
        }

        protected override void UpdatePartFromModel()
        {
            if (m_ConnectorLabel != null)
            {
                m_ConnectorLabel.text = (m_Model as IHasTitle)?.DisplayTitle ?? String.Empty;
            }

            ShowCap();
        }

        void OnMouseEnter(MouseEnterEvent evt)
        {
            m_Hovering = true;
            ShowCap();
        }

        void OnMouseLeave(MouseLeaveEvent evt)
        {
            m_Hovering = false;
            ShowCap();
        }

        void ShowCap()
        {
            if (m_ConnectorBoxCap != null)
            {
                bool showCap = m_Hovering || ((m_OwnerElement as VisualElement)?.ClassListContains(Port.willConnectModifierUssClassName) ?? false);

                if ((m_Model is IPortModel portModel && portModel.IsConnected()) || showCap)
                {
                    m_ConnectorBoxCap.style.visibility = StyleKeyword.Null;
                }
                else
                {
                    m_ConnectorBoxCap.style.visibility = Visibility.Hidden;
                }
            }
        }
    }
}
