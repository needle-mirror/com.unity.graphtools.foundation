using System;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    [GraphElementsExtensionMethodsCache]
    public static class ConstantEditorExtensions
    {
        public static readonly string k_UssClassName = "ge-inline-value-editor";

        public static void TriggerOnValueChanged<T>(this IConstantEditorBuilder builder, T oldValue, T newValue)
        {
            using (ChangeEvent<T> other = ChangeEvent<T>.GetPooled(oldValue, newValue))
                builder.OnValueChanged(other);
        }

        static VisualElement BuildInlineValueEditor<T>(this IConstantEditorBuilder builder, T oldValue, BaseField<T> field)
        {
            var root = new VisualElement();

            root.AddStylesheet("InlineValueEditor.uss");
            root.AddToClassList(k_UssClassName);
            //Mimic UIElement property fields style
            root.AddToClassList("unity-property-field");

            field.value = oldValue;
            root.Add(field);
            field.RegisterValueChangedCallback(evt => builder.OnValueChanged(evt));
            return root;
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

        static VisualElement BuildEnumFieldEditor(EnumValueReference enumConstant, Action<Enum> onNewEnumValue)
        {
            var enumEditor = new EnumField(enumConstant.ValueAsEnum());
            enumEditor.RegisterValueChangedCallback(evt =>
            {
                onNewEnumValue(evt.newValue);
            });
            return enumEditor;
        }

        public static VisualElement BuildColorEditor(this IConstantEditorBuilder builder, Color c)
        {
            return builder.BuildInlineValueEditor(c, new ColorField());
        }

        public static VisualElement BuildFloatEditor(this IConstantEditorBuilder builder, float f)
        {
            return builder.BuildInlineValueEditor(f, new FloatField());
        }

        public static VisualElement BuildDoubleEditor(this IConstantEditorBuilder builder, double d)
        {
            return builder.BuildInlineValueEditor(d, new DoubleField());
        }

        public static VisualElement BuildIntEditor(this IConstantEditorBuilder builder, int i)
        {
            return builder.BuildInlineValueEditor(i, new IntegerField());
        }

        public static VisualElement BuildBoolEditor(this IConstantEditorBuilder builder, bool b)
        {
            return builder.BuildInlineValueEditor(b, new Toggle());
        }

        public static VisualElement BuildStringEditor(this IConstantEditorBuilder builder, string s)
        {
            return builder.BuildInlineValueEditor(s, new TextField());
        }

        public static VisualElement BuildFloatEditor(this IConstantEditorBuilder builder, Vector2 f)
        {
            return builder.BuildInlineValueEditor(f, new Vector2Field());
        }

        public static VisualElement BuildFloatEditor(this IConstantEditorBuilder builder, Vector3 f)
        {
            return builder.BuildInlineValueEditor(f, new Vector3Field());
        }

        public static VisualElement BuildFloatEditor(this IConstantEditorBuilder builder, Vector4 f)
        {
            return builder.BuildInlineValueEditor(f, new Vector4Field());
        }

        public static VisualElement BuildFloatEditor(this IConstantEditorBuilder builder, Quaternion f)
        {
            return null;
        }

        public static VisualElement BuildEnumEditor(this IConstantEditorBuilder builder, EnumValueReference enumConstant)
        {
            void TriggerOnValueChange(Enum newEnumValue)
            {
                var oldValue = enumConstant;
                var newValue = new EnumValueReference(newEnumValue);
                using (ChangeEvent<EnumValueReference> other = ChangeEvent<EnumValueReference>.GetPooled(oldValue, newValue))
                    builder.OnValueChanged(other);
            }

            Type enumType = enumConstant.EnumType.Resolve();
            VisualElement editor = enumType == typeof(KeyCode)
                ? BuildSearcherEnumEditor(enumConstant.ValueAsEnum(), enumType, TriggerOnValueChange)
                : BuildEnumFieldEditor(enumConstant, TriggerOnValueChange);

            editor.SetEnabled(!builder.ConstantIsLocked);
            return editor;
        }
    }
}
