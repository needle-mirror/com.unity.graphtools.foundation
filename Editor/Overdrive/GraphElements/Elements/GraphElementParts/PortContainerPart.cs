using System;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.GraphElements
{
    public class PortContainerPart : BaseGraphElementPart
    {
        public static PortContainerPart Create(string name, IGTFGraphElementModel model, IGraphElement graphElement, string parentClassName)
        {
            if (model is IHasPorts)
            {
                return new PortContainerPart(name, model, graphElement, parentClassName);
            }

            return null;
        }

        public static readonly string k_UssClassName = "ge-port-container-part";
        public static readonly string k_PortsUssName = "ports";

        protected PortContainerPart(string name, IGTFGraphElementModel model, IGraphElement ownerElement, string parentClassName)
            : base(name, model, ownerElement, parentClassName) {}

        PortContainer PortContainer { get; set; }
        VisualElement m_Root;

        public override VisualElement Root => m_Root;

        protected override void BuildPartUI(VisualElement container)
        {
            if (m_Model is IHasPorts)
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
            if (m_Model is IHasPorts portHolder)
            {
                PortContainer?.UpdatePorts(portHolder.Ports, m_OwnerElement.GraphView, m_OwnerElement.Store);
            }
        }
    }
}
