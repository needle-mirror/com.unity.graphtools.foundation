using System;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public class InOutPortContainerPart : BaseGraphElementPart
    {
        public static readonly string ussClassName = "ge-in-out-port-container-part";
        public static readonly string inputPortsUssName = "inputs";
        public static readonly string outputPortsUssName = "outputs";

        public static InOutPortContainerPart Create(string name, IGraphElementModel model, IGraphElement graphElement, string parentClassName)
        {
            if (model is IPortNode)
            {
                return new InOutPortContainerPart(name, model, graphElement, parentClassName);
            }

            return null;
        }

        protected PortContainer m_InputPortContainer;

        protected PortContainer m_OutputPortContainer;

        VisualElement m_Root;

        public override VisualElement Root => m_Root;

        protected InOutPortContainerPart(string name, IGraphElementModel model, IGraphElement ownerElement, string parentClassName)
            : base(name, model, ownerElement, parentClassName) {}

        protected override void BuildPartUI(VisualElement container)
        {
            if (m_Model is IPortNode)
            {
                m_Root = new VisualElement { name = PartName };
                m_Root.AddToClassList(ussClassName);
                m_Root.AddToClassList(m_ParentClassName.WithUssElement(PartName));

                m_InputPortContainer = new PortContainer { name = inputPortsUssName };
                m_InputPortContainer.AddToClassList(m_ParentClassName.WithUssElement(inputPortsUssName));
                m_Root.Add(m_InputPortContainer);

                m_OutputPortContainer = new PortContainer { name = outputPortsUssName };
                m_OutputPortContainer.AddToClassList(m_ParentClassName.WithUssElement(outputPortsUssName));
                m_Root.Add(m_OutputPortContainer);

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
            switch (m_Model)
            {
                // TODO: Reinstate.
                // case ISingleInputPortNode inputPortHolder:
                //     m_InputPortContainer?.UpdatePorts(new[] { inputPortHolder.InputPort }, m_OwnerElement.GraphView, m_OwnerElement.Store);
                //     break;
                // case ISingleOutputPortNode outputPortHolder:
                //     m_OutputPortContainer?.UpdatePorts(new[] { outputPortHolder.OutputPort }, m_OwnerElement.GraphView, m_OwnerElement.Store);
                //     break;
                case IInOutPortsNode portHolder:
                    m_InputPortContainer?.UpdatePorts(portHolder.GetInputPorts(), m_OwnerElement.GraphView, m_OwnerElement.Store);
                    m_OutputPortContainer?.UpdatePorts(portHolder.GetOutputPorts(), m_OwnerElement.GraphView, m_OwnerElement.Store);
                    break;
            }
        }
    }
}
