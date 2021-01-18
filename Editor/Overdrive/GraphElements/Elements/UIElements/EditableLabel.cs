using UnityEditor.GraphToolsFoundation.Overdrive.Bridge;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public class EditableLabel : VisualElementBridge
    {
        public new class UxmlFactory : UxmlFactory<EditableLabel, UxmlTraits> {}
        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            UxmlBoolAttributeDescription m_Multiline = new UxmlBoolAttributeDescription { name = "multiline" };

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                EditableLabel field = ((EditableLabel)ve);
                field.multiline = m_Multiline.GetValueFromBag(bag, cc);
                base.Init(ve, bag, cc);
            }
        }

        public static readonly string labelName = "label";
        public static readonly string textFieldName = "text-field";

        Label m_Label;

        TextField m_TextField;

        string m_CurrentValue;

        ContextualMenuManipulator m_ContextualMenuManipulator;

        protected ContextualMenuManipulator ContextualMenuManipulator
        {
            get => m_ContextualMenuManipulator;
            set => this.ReplaceManipulator(ref m_ContextualMenuManipulator, value);
        }

        public bool multiline
        {
            set => m_TextField.multiline = value;
        }

        public EditableLabel()
        {
            SetIsCompositeRoot();
            focusable = true;

            GraphElementHelper.LoadTemplateAndStylesheet(this, "EditableLabel", "ge-editable-label");

            m_Label = this.Q<Label>(name: labelName);
            m_TextField = this.Q<TextField>(name: textFieldName);

            m_Label.RegisterCallback<MouseDownEvent>(OnLabelMouseDown);

            m_TextField.style.display = DisplayStyle.None;
            m_TextField.RegisterCallback<KeyDownEvent>(OnKeyDown);
            m_TextField.RegisterCallback<BlurEvent>(OnFieldBlur);
            m_TextField.RegisterCallback<ChangeEvent<string>>(OnChange);
            m_TextField.isDelayed = true;

            ContextualMenuManipulator = new ContextualMenuManipulator(BuildContextualMenu);
        }

        public void SetValueWithoutNotify(string value)
        {
            ((INotifyValueChanged<string>)m_Label).SetValueWithoutNotify(value);
            m_TextField.SetValueWithoutNotify(value);
        }

        protected virtual void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            if (evt.menu.MenuItems().Count > 0)
                evt.menu.AppendSeparator();

            if (m_TextField.style.display == DisplayStyle.None)
            {
                evt.menu.AppendAction("Edit", action => BeginEditing());
            }
        }

        public void BeginEditing()
        {
            m_CurrentValue = m_Label.text;

            m_Label.style.display = DisplayStyle.None;
            m_TextField.style.display = StyleKeyword.Null;

            m_TextField.Q(TextField.textInputUssName).Focus();
            m_TextField.SelectAll();
        }

        void OnLabelMouseDown(MouseDownEvent e)
        {
            if (e.target == e.currentTarget)
            {
                if (e.button == (int)MouseButton.LeftMouse)
                {
                    if (e.clickCount == 1)
                    {
                        // Prevent focusing on single click.
                        e.PreventDefault();
                    }
                    else if (e.clickCount == 2)
                    {
                        BeginEditing();

                        e.StopPropagation();
                        e.PreventDefault();
                    }
                }
            }
        }

        void OnKeyDown(KeyDownEvent e)
        {
            if (e.target == e.currentTarget)
            {
                if (e.keyCode == KeyCode.Escape)
                {
                    m_TextField.SetValueWithoutNotify(m_CurrentValue);
                    m_TextField.Blur();
                }
            }
        }

        void OnFieldBlur(BlurEvent e)
        {
            if (e.target == e.currentTarget)
                m_Label.style.display = StyleKeyword.Null;
            m_TextField.style.display = DisplayStyle.None;
        }

        void OnChange(ChangeEvent<string> e)
        {
            if (e.target == e.currentTarget)
                ((INotifyValueChanged<string>)m_Label).SetValueWithoutNotify(m_TextField.value);
        }
    }
}
