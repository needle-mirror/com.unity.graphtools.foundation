using System;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.GraphElements
{
    public class PortConnectorPart : BaseGraphElementPart
    {
        public static readonly string k_UssClassName = "ge-port-connector-part";
        public static readonly string k_ConnectorUssName = "connector";
        public static readonly string k_ConnectorCapUssName = "cap";
        public static readonly string k_LabelName = "label";

        public static PortConnectorPart Create(string name, IGTFGraphElementModel model, IGraphElement graphElement, string parentClassName)
        {
            if (model is IGTFPortModel)
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

        protected PortConnectorPart(string name, IGTFGraphElementModel model, IGraphElement ownerElement, string parentClassName)
            : base(name, model, ownerElement, parentClassName) {}

        protected override void BuildPartUI(VisualElement container)
        {
            m_Root = new VisualElement { name = PartName };
            m_Root.AddToClassList(k_UssClassName);
            m_Root.AddToClassList(m_ParentClassName.WithUssElement(PartName));

            m_ConnectorBox = new VisualElement { name = k_ConnectorUssName };
            m_ConnectorBox.AddToClassList(k_UssClassName.WithUssElement(k_ConnectorUssName));
            m_ConnectorBox.AddToClassList(m_ParentClassName.WithUssElement(k_ConnectorUssName));
            m_Root.Add(m_ConnectorBox);

            m_ConnectorBoxCap = new VisualElement { name = k_ConnectorCapUssName };
            m_ConnectorBoxCap.AddToClassList(k_UssClassName.WithUssElement(k_ConnectorCapUssName));
            m_ConnectorBoxCap.AddToClassList(m_ParentClassName.WithUssElement(k_ConnectorCapUssName));
            m_ConnectorBox.Add(m_ConnectorBoxCap);

            if (m_Model is IHasTitle)
            {
                m_ConnectorLabel = new Label { name = k_LabelName };
                m_ConnectorLabel.AddToClassList(k_UssClassName.WithUssElement(k_LabelName));
                m_ConnectorLabel.AddToClassList(m_ParentClassName.WithUssElement(k_LabelName));
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
                bool showCap = m_Hovering || ((m_OwnerElement as VisualElement)?.ClassListContains(Port.k_WillConnectModifierUssClassName) ?? false);

                if ((m_Model is IGTFPortModel portModel && portModel.IsConnected()) || showCap)
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
