using System;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.GraphElements
{
    public class InOutPortContainerPart : BaseGraphElementPart
    {
        public static InOutPortContainerPart Create(string name, IGTFGraphElementModel model, IGraphElement graphElement, string parentClassName)
        {
            if (model is IHasPorts)
            {
                return new InOutPortContainerPart(name, model, graphElement, parentClassName);
            }

            return null;
        }

        public static readonly string k_UssClassName = "ge-in-out-port-container-part";
        public static readonly string k_InputPortsUssName = "inputs";
        public static readonly string k_OutputPortsUssName = "outputs";

        PortContainer m_InputPortContainer;
        PortContainer m_OutputPortContainer;
        VisualElement m_Root;

        public override VisualElement Root => m_Root;

        protected InOutPortContainerPart(string name, IGTFGraphElementModel model, IGraphElement ownerElement, string parentClassName)
            : base(name, model, ownerElement, parentClassName) {}

        protected override void BuildPartUI(VisualElement container)
        {
            if (m_Model is IHasPorts)
            {
                m_Root = new VisualElement { name = PartName };
                m_Root.AddToClassList(k_UssClassName);
                m_Root.AddToClassList(m_ParentClassName.WithUssElement(PartName));

                m_InputPortContainer = new PortContainer { name = k_InputPortsUssName };
                m_InputPortContainer.AddToClassList(m_ParentClassName.WithUssElement(k_InputPortsUssName));
                m_Root.Add(m_InputPortContainer);

                m_OutputPortContainer = new PortContainer { name = k_OutputPortsUssName };
                m_OutputPortContainer.AddToClassList(m_ParentClassName.WithUssElement(k_OutputPortsUssName));
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
            if (m_Model is IHasIOPorts portHolder)
            {
                m_InputPortContainer?.UpdatePorts(portHolder.InputPorts, m_OwnerElement.GraphView, m_OwnerElement.Store);
                m_OutputPortContainer?.UpdatePorts(portHolder.OutputPorts, m_OwnerElement.GraphView, m_OwnerElement.Store);
            }
            else if (m_Model is IHasSingleInputPort inputPortHolder)
            {
                m_InputPortContainer?.UpdatePorts(new[] { inputPortHolder.InputPort }, m_OwnerElement.GraphView, m_OwnerElement.Store);
            }
            else if (m_Model is IHasSingleOutputPort outputPortHolder)
            {
                m_OutputPortContainer?.UpdatePorts(new[] { outputPortHolder.OutputPort }, m_OwnerElement.GraphView, m_OwnerElement.Store);
            }
        }
    }
}
