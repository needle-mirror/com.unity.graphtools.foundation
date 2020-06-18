using System;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.GraphElements
{
    public class FourWayResizerPart : BaseGraphElementPart
    {
        public static FourWayResizerPart Create(string name, IGTFGraphElementModel model, IGraphElement graphElement, string parentClassName)
        {
            if (model is IResizable)
            {
                return new FourWayResizerPart(name, model, graphElement, parentClassName);
            }

            return null;
        }

        protected FourWayResizerPart(string name, IGTFGraphElementModel model, IGraphElement ownerElement, string parentClassName)
            : base(name, model, ownerElement, parentClassName) {}

        ResizableElement m_ResizableElement;
        public override VisualElement Root => m_ResizableElement;

        protected override void BuildPartUI(VisualElement container)
        {
            if (m_Model is IResizable)
            {
                m_ResizableElement = new ResizableElement { name = PartName };
                m_ResizableElement.AddToClassList(m_ParentClassName.WithUssElement(PartName));
                container.Add(m_ResizableElement);
            }
        }

        protected override void UpdatePartFromModel()
        {
            if ((m_Model as IResizable)?.IsResizable ?? false)
            {
                if (m_ResizableElement != null)
                    m_ResizableElement.style.visibility = StyleKeyword.Null;
            }
            else
            {
                if (m_ResizableElement != null)
                    m_ResizableElement.style.visibility = Visibility.Hidden;
            }
        }
    }
}
