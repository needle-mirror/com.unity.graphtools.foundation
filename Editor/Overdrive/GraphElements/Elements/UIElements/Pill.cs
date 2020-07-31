using System.Collections.Generic;
using UnityEditor.GraphToolsFoundation.Overdrive.Bridge;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.GraphElements
{
    public class Pill : VisualElement
    {
        public new class UxmlFactory : UxmlFactory<Pill, UxmlTraits> {}

        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            UxmlBoolAttributeDescription m_Highlighted = new UxmlBoolAttributeDescription { name = "highlighted" };
            UxmlStringAttributeDescription m_Text = new UxmlStringAttributeDescription { name = "text" };

            public override IEnumerable<UxmlChildElementDescription> uxmlChildElementsDescription
            {
                get { yield break; }
            }

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);
                ((Pill)ve).highlighted = m_Highlighted.GetValueFromBag(bag, cc);
                ((Pill)ve).text = m_Text.GetValueFromBag(bag, cc);
            }
        }

        readonly Label m_TitleLabel;
        readonly Image m_Icon;
        VisualElement m_Left;
        readonly VisualElement m_LeftContainer;
        VisualElement m_Right;
        readonly VisualElement m_RightContainer;
        bool m_Highlighted;

        public bool highlighted
        {
            get { return m_Highlighted; }
            set
            {
                if (m_Highlighted == value)
                {
                    return;
                }

                m_Highlighted = value;

                if (m_Highlighted)
                    AddToClassList("highlighted");
                else
                    RemoveFromClassList("highlighted");
            }
        }

        public string text
        {
            get { return m_TitleLabel.text; }
            set { m_TitleLabel.text = value; }
        }

        public Texture icon
        {
            get { return m_Icon != null ? m_Icon.image : null; }
            set
            {
                if (m_Icon == null || m_Icon.image == value)
                    return;

                m_Icon.image = value;

                UpdateIconVisibility();
            }
        }

        public VisualElement left
        {
            get { return m_Left; }
            set
            {
                if (m_Left == value)
                    return;

                if (m_Left != null)
                    m_LeftContainer.Remove(m_Left);

                m_Left = value;

                if (m_Left != null)
                    m_LeftContainer.Add(m_Left);

                UpdateVisibility();
            }
        }

        public VisualElement right
        {
            get { return m_Right; }
            set
            {
                if (m_Right == value)
                    return;

                if (m_Right != null)
                    m_RightContainer.Remove(m_Right);

                m_Right = value;

                if (m_Right != null)
                    m_RightContainer.Add(m_Right);

                UpdateVisibility();
            }
        }

        void UpdateIconVisibility()
        {
            if (icon == null)
            {
                RemoveFromClassList("has-icon");
                m_Icon.style.visibility = Visibility.Hidden;
            }
            else
            {
                AddToClassList("has-icon");
                m_Icon.style.visibility = StyleKeyword.Null;
            }
        }

        void UpdateVisibility()
        {
            if (m_Left != null)
            {
                AddToClassList("has-left");
                if (m_LeftContainer != null) m_LeftContainer.style.visibility = StyleKeyword.Null;
            }
            else
            {
                RemoveFromClassList("has-left");
                if (m_LeftContainer != null) m_LeftContainer.style.visibility = Visibility.Hidden;
            }

            if (m_Right != null)
            {
                AddToClassList("has-right");
                if (m_RightContainer != null) m_RightContainer.style.visibility = StyleKeyword.Null;
            }
            else
            {
                RemoveFromClassList("has-right");
                if (m_RightContainer != null) m_RightContainer.style.visibility = Visibility.Hidden;
            }
        }

        public Pill()
        {
            this.AddStylesheet("Pill.uss");

            var tpl = GraphElementHelper.LoadUXML("Pill.uxml");
            VisualElement mainContainer = tpl.Instantiate();

            m_TitleLabel = mainContainer.Q<Label>("title-label");
            m_Icon = mainContainer.Q<Image>("icon");
            m_LeftContainer = mainContainer.Q("input");
            m_RightContainer = mainContainer.Q("output");

            Add(mainContainer);

            AddToClassList("pill");

            UpdateVisibility();
            UpdateIconVisibility();
        }

        public Pill(VisualElement left, VisualElement right) : this()
        {
            this.left = left;
            this.right = right;

            UpdateVisibility();
            UpdateIconVisibility();
        }
    }
}
