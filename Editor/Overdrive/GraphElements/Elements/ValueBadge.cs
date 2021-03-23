using System;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public class ValueBadge : Badge
    {
        public new static readonly string ussClassName = "ge-value-badge";
        static VisualTreeAsset s_ValueTemplate;

        Label m_TextElement;
        Image m_Image;

        void SetBadgeColor(Color color)
        {
            m_Image.tintColor = color;

            style.borderLeftColor = color;
            style.borderRightColor = color;
            style.borderTopColor = color;
            style.borderBottomColor = color;
        }

        protected override void BuildElementUI()
        {
            base.BuildElementUI();

            if (s_ValueTemplate == null)
                s_ValueTemplate = GraphElementHelper.LoadUXML("ValueBadge.uxml");

            s_ValueTemplate.CloneTree(this);
            m_TextElement = this.SafeQ<Label>("desc");
            m_Image = this.SafeQ<Image>();
        }

        protected override void PostBuildUI()
        {
            base.PostBuildUI();

            AddToClassList(ussClassName);
        }

        protected override void UpdateElementFromModel()
        {
            base.UpdateElementFromModel();

            var valueModel = BadgeModel as IValueBadgeModel;
            Assert.IsNotNull(valueModel);

            var portModel = BadgeModel.ParentModel;
            Assert.IsNotNull(portModel);
            var port = portModel.GetUI<Port>(GraphView);
            Assert.IsNotNull(port);

            SetBadgeColor(port.PortColor);
            m_TextElement.text = valueModel.DisplayValue;
        }

        protected override void Attach()
        {
            var valueModel = Model as IValueBadgeModel;
            var portModel = valueModel?.ParentPortModel;
            var port = portModel?.GetUI<Port>(GraphView);
            var cap = port?.SafeQ(className: "ge-port__cap") ?? port;
            if (cap != null)
            {
                var alignment = portModel.Direction == Direction.Output ? SpriteAlignment.BottomRight : SpriteAlignment.BottomLeft;
                AttachTo(cap, alignment);
            }
        }
    }
}
