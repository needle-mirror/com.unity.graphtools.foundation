using System;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public class PortContainerPart : BaseGraphElementPart
    {
        public static readonly string k_UssClassName = "ge-port-container-part";
        public static readonly string k_PortsUssName = "ports";

        public static PortContainerPart Create(string name, IGraphElementModel model, IGraphElement graphElement, string parentClassName)
        {
            if (model is IPortNode)
            {
                return new PortContainerPart(name, model, graphElement, parentClassName);
            }

            return null;
        }

        VisualElement m_Root;

        PortContainer PortContainer { get; set; }

        public override VisualElement Root => m_Root;

        protected PortContainerPart(string name, IGraphElementModel model, IGraphElement ownerElement, string parentClassName)
            : base(name, model, ownerElement, parentClassName) {}

        protected override void BuildPartUI(VisualElement container)
        {
            if (m_Model is IPortNode)
            {
                m_Root = new VisualElement { name = PartName };
                m_Root.AddToClassList(k_UssClassName);
                m_Root.AddToClassList(m_ParentClassName.WithUssElement(PartName));

                PortContainer = new PortContainer { name = k_PortsUssName };
                PortContainer.AddToClassList(m_ParentClassName.WithUssElement(k_PortsUssName));
                m_Root.Add(PortContainer);

                container.Add(m_Root);
            }
        }

        protected override void PostBuildPartUI()
        {
            base.PostBuildPartUI();
            m_Root.AddStylesheet("PortContainerPart.uss");
        }

        protected override void UpdatePartFromModel()
        {
            if (m_Model is IPortNode portHolder)
            {
                PortContainer?.UpdatePorts(portHolder.Ports, m_OwnerElement.GraphView, m_OwnerElement.Store);
            }
        }
    }
}
