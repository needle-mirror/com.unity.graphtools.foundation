#if DISABLE_SIMPLE_MATH_TESTS
using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Editor.UsingDataModel.NoPresenters
{
    class BlackboardFieldPropertyView : VisualElement
    {
        MathBookField m_Field;
        Toggle m_ExposedToggle;
        TextField m_ValueField;
        Label m_ValueLabel;
        TextField m_TooltipField;
        bool m_NotificationsBlocked = false;

        void AddRow(string labelText, VisualElement control)
        {
            VisualElement rowView = new VisualElement();

            rowView.AddToClassList("rowView");

            Label label = new Label(labelText);

            label.AddToClassList("rowViewLabel");
            rowView.Add(label);

            control.AddToClassList("rowViewControl");
            rowView.Add(control);

            Add(rowView);
        }

        public BlackboardFieldPropertyView(MathBookField field)
        {
            m_Field = field;
            m_ExposedToggle = new Toggle();
            m_ExposedToggle.RegisterValueChangedCallback(OnExposedToggle);
            AddRow("Exposed", m_ExposedToggle);

            if (field.direction == MathBookField.Direction.Input)
            {
                AddRow("Value", m_ValueField = new TextField());
                m_ValueField.RegisterCallback<ChangeEvent<string>>(e => OnDefaultValueChanged());
            }
            else
            {
                AddRow("Value", m_ValueLabel = new Label());
            }

            AddRow("Tooltip", m_TooltipField = new TextField());

            AddToClassList("blackboardFieldPropertyView");

            UpdateData();

            field.changed += (e, c) => UpdateData();
            m_TooltipField.RegisterCallback<ChangeEvent<string>>(e => OnToolTipChanged());
        }

        void OnExposedToggle(ChangeEvent<bool> evt)
        {
            if (m_NotificationsBlocked)
                return;

            m_Field.exposed = m_ExposedToggle.value;
        }

        public void OnDefaultValueChanged()
        {
            if (m_NotificationsBlocked || m_ValueField == null)
                return;

            float parsedValue = 0;

            if (float.TryParse(m_ValueField.text, out parsedValue))
            {
                m_Field.value = parsedValue;
            }
        }

        public void OnToolTipChanged()
        {
            if (m_NotificationsBlocked)
                return;

            m_Field.toolTip = m_TooltipField.text;
        }

        void UpdateData()
        {
            if (m_NotificationsBlocked)
                return;

            m_NotificationsBlocked = true;
            m_ExposedToggle.value = m_Field.exposed;

            if (m_ValueField != null)
                m_ValueField.value = m_Field.value.ToString();

            if (m_ValueLabel != null)
                m_ValueLabel.text = m_Field.value.ToString();

            m_TooltipField.value = m_Field.toolTip;
            m_NotificationsBlocked = false;
        }
    }
}
#endif
