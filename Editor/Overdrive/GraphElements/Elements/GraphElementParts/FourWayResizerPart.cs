using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public class FourWayResizerPart : BaseModelUIPart
    {
        public static FourWayResizerPart Create(string name, IGraphElementModel model, IModelUI modelUI, string parentClassName)
        {
            if (model is IResizable)
            {
                return new FourWayResizerPart(name, model, modelUI, parentClassName);
            }

            return null;
        }

        public override VisualElement Root => m_ResizableElement;

        ResizableElement m_ResizableElement;

        protected FourWayResizerPart(string name, IGraphElementModel model, IModelUI ownerElement, string parentClassName)
            : base(name, model, ownerElement, parentClassName) { }

        protected override void BuildPartUI(VisualElement container)
        {
            if (m_Model?.IsResizable() ?? false)
            {
                m_ResizableElement = new ResizableElement { name = PartName };
                m_ResizableElement.AddToClassList(m_ParentClassName.WithUssElement(PartName));
                container.Add(m_ResizableElement);
            }
        }

        protected override void UpdatePartFromModel()
        {
            if (m_Model?.IsResizable() ?? false)
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
