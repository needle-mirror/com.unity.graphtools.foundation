using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.GraphElements
{
    public class CollapseButton : VisualElement, INotifyValueChanged<bool>
    {
        bool m_Collapsed;

        public static readonly string k_UssClassName = "ge-collapse-button";
        public static readonly string k_CollapsedUssClassName = k_UssClassName.WithUssModifier("collapsed");

        public static readonly string k_IconElementName = "icon";
        public static readonly string k_IconElementUssClassName = k_UssClassName.WithUssElement(k_IconElementName);

        public CollapseButton()
        {
            m_Collapsed = false;

            this.AddStylesheet("CollapseButton.uss");
            AddToClassList(k_UssClassName);

            var icon = new VisualElement { name = k_IconElementName };
            icon.AddToClassList(k_IconElementUssClassName);
            Add(icon);

            var clickable = new Clickable(OnClick);
            this.AddManipulator(clickable);
        }

        void OnClick()
        {
            value = !m_Collapsed;
        }

        public void SetValueWithoutNotify(bool newValue)
        {
            m_Collapsed = newValue;
            EnableInClassList(k_CollapsedUssClassName, m_Collapsed);
        }

        public bool value
        {
            get => m_Collapsed;
            set
            {
                if (m_Collapsed != value)
                {
                    using (var e = ChangeEvent<bool>.GetPooled(m_Collapsed, value))
                    {
                        e.target = this;
                        SetValueWithoutNotify(value);
                        SendEvent(e);
                    }
                }
            }
        }
    }
}
