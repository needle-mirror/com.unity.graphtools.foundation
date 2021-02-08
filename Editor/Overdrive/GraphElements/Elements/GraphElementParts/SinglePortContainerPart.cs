using System;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public class SinglePortContainerPart : BaseModelUIPart
    {
        public static SinglePortContainerPart Create(string name, IGraphElementModel model, IModelUI modelUI, string parentClassName)
        {
            if (model is IPortModel)
            {
                return new SinglePortContainerPart(name, model, modelUI, parentClassName);
            }

            return null;
        }

        PortContainer m_PortContainer;

        public override VisualElement Root => m_PortContainer;

        protected SinglePortContainerPart(string name, IGraphElementModel model, IModelUI ownerElement, string parentClassName)
            : base(name, model, ownerElement, parentClassName) {}

        protected override void BuildPartUI(VisualElement container)
        {
            if (m_Model is IPortModel)
            {
                m_PortContainer = new PortContainer { name = PartName };
                m_PortContainer.AddToClassList(m_ParentClassName.WithUssElement(PartName));
                container.Add(m_PortContainer);
            }
        }

        protected override void UpdatePartFromModel()
        {
            if (m_Model is IPortModel portModel)
                m_PortContainer?.UpdatePorts(new[] { portModel }, m_OwnerElement.GraphView, m_OwnerElement.CommandDispatcher);
        }
    }
}
