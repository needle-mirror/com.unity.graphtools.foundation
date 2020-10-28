using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public class PortConnectorWithIconPart : PortConnectorPart
    {
        public static readonly string k_IconUssName = "icon";

        public new static PortConnectorWithIconPart Create(string name, IGraphElementModel model, IGraphElement graphElement, string parentClassName)
        {
            if (model is IPortModel && graphElement is Port)
            {
                return new PortConnectorWithIconPart(name, model, graphElement, parentClassName);
            }

            return null;
        }

        Image m_Icon;

        protected PortConnectorWithIconPart(string name, IGraphElementModel model, IGraphElement ownerElement, string parentClassName)
            : base(name, model, ownerElement, parentClassName) {}

        protected override void BuildPartUI(VisualElement container)
        {
            base.BuildPartUI(container);

            m_Icon = new Image();
            m_Icon.AddToClassList(m_ParentClassName.WithUssElement(k_IconUssName));
            m_Icon.tintColor = (m_OwnerElement as Port)?.PortColor ?? Color.white;
            Root.Insert(1, m_Icon);
        }

        protected override void PostBuildPartUI()
        {
            base.PostBuildPartUI();
            Root.AddStylesheet("PortConnectorWithIconPart.uss");
        }

        protected override void UpdatePartFromModel()
        {
            base.UpdatePartFromModel();
            m_Icon.tintColor = (m_OwnerElement as Port)?.PortColor ?? Color.white;
        }
    }
}
