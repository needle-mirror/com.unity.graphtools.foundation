using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public class CollapseButton : VisualElement, INotifyValueChanged<bool>
    {
        public static readonly string ussClassName = "ge-collapse-button";
        public static readonly string collapsedUssClassName = ussClassName.WithUssModifier("collapsed");

        public static readonly string iconElementName = "icon";
        public static readonly string iconElementUssClassName = ussClassName.WithUssElement(iconElementName);

        bool m_Collapsed;

        Clickable m_Clickable;

        protected Clickable Clickable
        {
            get => m_Clickable;
            set => this.ReplaceManipulator(ref m_Clickable, value);
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

        public CollapseButton()
        {
            m_Collapsed = false;

            this.AddStylesheet("CollapseButton.uss");
            AddToClassList(ussClassName);

            var icon = new VisualElement { name = iconElementName };
            icon.AddToClassList(iconElementUssClassName);
            Add(icon);

            Clickable = new Clickable(OnClick);
        }

        void OnClick()
        {
            value = !m_Collapsed;
        }

        public void SetValueWithoutNotify(bool newValue)
        {
            m_Collapsed = newValue;
            EnableInClassList(collapsedUssClassName, m_Collapsed);
        }
    }
}
