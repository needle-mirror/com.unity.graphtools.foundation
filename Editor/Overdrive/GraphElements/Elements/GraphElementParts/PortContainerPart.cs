using System;
using System.Linq;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public class PortContainerPart : BaseModelUIPart
    {
        public static readonly string ussClassName = "ge-port-container-part";
        public static readonly string portsUssName = "ports";

        public static PortContainerPart Create(string name, IGraphElementModel model, IModelUI modelUI, string parentClassName)
        {
            if (model is IPortNodeModel)
            {
                return new PortContainerPart(name, model, modelUI, parentClassName);
            }

            return null;
        }

        VisualElement m_Root;

        PortContainer PortContainer { get; set; }

        public override VisualElement Root => m_Root;

        protected PortContainerPart(string name, IGraphElementModel model, IModelUI ownerElement, string parentClassName)
            : base(name, model, ownerElement, parentClassName) { }

        protected override void BuildPartUI(VisualElement container)
        {
            if (m_Model is IPortNodeModel portHolder)
            {
                m_Root = new VisualElement { name = PartName };
                m_Root.AddToClassList(ussClassName);
                m_Root.AddToClassList(m_ParentClassName.WithUssElement(PartName));

                PortContainer = new PortContainer { name = portsUssName };
                PortContainer.AddToClassList(m_ParentClassName.WithUssElement(portsUssName));
                m_Root.Add(PortContainer);

                var ports = portHolder.Ports.Where(p => p.Orientation == Orientation.Horizontal);
                PortContainer?.UpdatePorts(ports, m_OwnerElement.GraphView, m_OwnerElement.CommandDispatcher);

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
            if (m_Model is IPortNodeModel portHolder)
            {
                var ports = portHolder.Ports.Where(p => p.Orientation == Orientation.Horizontal);
                PortContainer?.UpdatePorts(ports, m_OwnerElement.GraphView, m_OwnerElement.CommandDispatcher);
            }
        }
    }
}
