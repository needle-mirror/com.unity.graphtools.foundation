using System;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.GraphElements
{
    public class CollapseButtonPart : BaseGraphElementPart
    {
        public static CollapseButtonPart Create(string name, IGTFGraphElementModel model, IGraphElement graphElement, string parentClassName)
        {
            if (model is ICollapsible)
            {
                return new CollapseButtonPart(name, model, graphElement, parentClassName);
            }

            return null;
        }

        protected CollapseButtonPart(string name, IGTFGraphElementModel model, IGraphElement ownerElement, string parentClassName)
            : base(name, model, ownerElement, parentClassName) {}

        protected CollapseButton CollapseButton { get; set; }
        public override VisualElement Root => CollapseButton;

        protected override void BuildPartUI(VisualElement container)
        {
            if (m_Model is ICollapsible)
            {
                CollapseButton = new CollapseButton { name = PartName };
                CollapseButton.AddToClassList(m_ParentClassName.WithUssElement(PartName));
                container.Add(CollapseButton);
            }
        }

        protected override void UpdatePartFromModel()
        {
            if (CollapseButton != null)
            {
                var collapsed = (m_Model as ICollapsible)?.Collapsed ?? false;
                CollapseButton.SetValueWithoutNotify(collapsed);
            }
        }
    }
}
