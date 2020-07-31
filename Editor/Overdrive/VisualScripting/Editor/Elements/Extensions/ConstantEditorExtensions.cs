using System;
using System.Collections.Generic;
using UnityEditor.GraphToolsFoundation.Overdrive.GraphElements;
using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.SmartSearch;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting
{
    [GraphElementsExtensionMethodsCache]
    public static class ConstantEditorExtensions
    {
        public static void TriggerOnValueChanged<T>(this IConstantEditorBuilder builder, T oldValue, T newValue)
        {
            using (ChangeEvent<T> other = ChangeEvent<T>.GetPooled(oldValue, newValue))
                builder.OnValueChanged(other);
        }

        public static VisualElement BuildEnumEditor(this IConstantEditorBuilder builder, EnumValueReference enumConstant)
        {
            void TriggerOnValueChange(Enum newEnumValue)
            {
                var oldValue = enumConstant;
                var newValue = new EnumValueReference(newEnumValue);
                builder.TriggerOnValueChanged(oldValue, newValue);
            }

            Type enumType = enumConstant.EnumType.Resolve();
            VisualElement editor = enumType == typeof(KeyCode)
                ? BuildSearcherEnumEditor(enumConstant.ValueAsEnum(), enumType, TriggerOnValueChange)
                : BuildFieldEnumEditor(enumConstant, TriggerOnValueChange);

            editor.SetEnabled(!builder.ConstantIsLocked);
            return editor;
        }

        static VisualElement BuildSearcherEnumEditor(Enum value, Type enumType, Action<Enum> onNewEnumValue)
        {
            var enumEditor = new Button { text = value.ToString() };
            enumEditor.clickable.clickedWithEventInfo += e =>
            {
                SearcherService.ShowEnumValues("Pick a value", enumType, e.originalMousePosition, (v, i) =>
                {
                    enumEditor.text = v.ToString();
                    onNewEnumValue(v);
                });
            };
            return enumEditor;
        }

        static VisualElement BuildFieldEnumEditor(EnumValueReference enumConstant, Action<Enum> onNewEnumValue)
        {
            var enumEditor = new EnumField(enumConstant.ValueAsEnum());
            enumEditor.RegisterValueChangedCallback(evt =>
            {
                onNewEnumValue(evt.newValue);
            });
            return enumEditor;
        }

        public static VisualElement BuildStringWrapperEditor(this IConstantEditorBuilder builder, IStringWrapperConstantModel icm)
        {
            var enumEditor = new Button { text = icm.ObjectValue.ToString() }; // TODO use a bindable element
            enumEditor.clickable.clickedWithEventInfo += e =>
            {
                List<string> allInputNames = icm.GetAllInputNames(builder.EditorDataModel as IEditorDataModel);
                SearcherService.ShowValues("Pick a value", allInputNames, e.originalMousePosition, (v, pickedIndex) =>
                {
                    var oldValue = icm.StringValue;
                    var newValue = v;
                    enumEditor.text = v;
                    builder.TriggerOnValueChanged(oldValue, newValue);
                });
            };
            enumEditor.SetEnabled(!builder.ConstantIsLocked);
            return enumEditor;
        }
    }
}
