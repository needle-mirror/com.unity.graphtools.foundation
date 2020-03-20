using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEditor.UIElements;
using UnityEditor.VisualScripting.Editor.SmartSearch;
using UnityEditor.VisualScripting.Model;
using UnityEditor.VisualScripting.Model.Stencils;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.VisualScripting.Editor.ConstantEditor
{
    [GraphtoolsExtensionMethods]
    public static class ConstantEditorExtensions
    {
        public static void TriggerOnValueChanged<T>(this IConstantEditorBuilder builder, T oldValue, T newValue)
        {
            using (ChangeEvent<T> other = ChangeEvent<T>.GetPooled(oldValue, newValue))
                builder.OnValueChanged(other);
        }

        public static VisualElement BuildEnumEditor(this IConstantEditorBuilder builder, EnumConstantNodeModel enumConstant)
        {
            void TriggerOnValueChange(Enum newEnumValue)
            {
                var oldValue = enumConstant.value;
                var newValue = enumConstant.value;
                newValue.Value = Convert.ToInt32(newEnumValue);
                builder.TriggerOnValueChanged(oldValue, newValue);
            }

            Type enumType = enumConstant.EnumType.Resolve(enumConstant.GraphModel.Stencil);
            VisualElement editor = enumType == typeof(KeyCode)
                ? BuildSearcherEnumEditor(enumConstant.EnumValue, enumType, TriggerOnValueChange)
                : BuildFieldEnumEditor(enumConstant, TriggerOnValueChange);

            editor.SetEnabled(!enumConstant.IsLocked);
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

        static VisualElement BuildFieldEnumEditor(EnumConstantNodeModel enumConstant, Action<Enum> onNewEnumValue)
        {
            var enumEditor = new EnumField(enumConstant.EnumValue);
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
                List<string> allInputNames = icm.GetAllInputNames();
                SearcherService.ShowValues("Pick a value", allInputNames, e.originalMousePosition, (v, pickedIndex) =>
                {
                    var oldValue = new InputName { name = icm.StringValue };
                    var newValue = oldValue;
                    newValue.name = v;
                    enumEditor.text = v;
                    builder.TriggerOnValueChanged(oldValue, newValue);
                });
            };
            enumEditor.SetEnabled(!icm.IsLocked);
            return enumEditor;
        }

        public static VisualElement BuildVector2Editor(this IConstantEditorBuilder builder, ConstantNodeModel<Vector2> v)
        {
            return builder.MakeFloatVectorEditor(v, 2,
                (vec, i) => vec[i], (ref Vector2 data, int i, float value) => data[i] = value);
        }

        public static VisualElement BuildVector3Editor(this IConstantEditorBuilder builder, ConstantNodeModel<Vector3> v)
        {
            return builder.MakeFloatVectorEditor(v, 3,
                (vec, i) => vec[i], (ref Vector3 data, int i, float value) => data[i] = value);
        }

        public static VisualElement BuildVector4Editor(this IConstantEditorBuilder builder, ConstantNodeModel<Vector4> v)
        {
            return builder.MakeFloatVectorEditor(v, 4,
                (vec, i) => vec[i], (ref Vector4 data, int i, float value) => data[i] = value);
        }

        public static VisualElement BuildQuaternionEditor(this IConstantEditorBuilder builder, ConstantNodeModel<Quaternion> q)
        {
            return builder.MakeFloatVectorEditor(q, 4,
                (vec, i) => vec[i], (ref Quaternion data, int i, float value) => data[i] = value);
        }

        static VisualElement BuildSingleFieldEditor<T>(this IConstantEditorBuilder builder, T oldValue, BaseField<T> field)
        {
            var root = new VisualElement();
            //Mimic UIElement property fields style
            root.AddToClassList("unity-property-field");
            root.styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>(UICreationHelper.templatePath + "ConstantEditors.uss"));
            field.value = oldValue;
            root.Add(field);
            field.RegisterValueChangedCallback(evt => builder.OnValueChanged(evt));
            return root;
        }

        public static VisualElement BuildColorEditor(this IConstantEditorBuilder builder, ConstantNodeModel<Color> c)
        {
            return builder.BuildSingleFieldEditor(c.value, new ColorField());
        }

        public static VisualElement BuildFloatEditor(this IConstantEditorBuilder builder, ConstantNodeModel<float> f)
        {
            return builder.BuildSingleFieldEditor(f.value, new FloatField());
        }

        public static VisualElement BuildDoubleEditor(this IConstantEditorBuilder builder, ConstantNodeModel<double> d)
        {
            return builder.BuildSingleFieldEditor(d.value, new DoubleField());
        }

        public static VisualElement BuildIntEditor(this IConstantEditorBuilder builder, ConstantNodeModel<int> i)
        {
            return builder.BuildSingleFieldEditor(i.value, new IntegerField());
        }

        public static VisualElement BuildBoolEditor(this IConstantEditorBuilder builder, ConstantNodeModel<bool> b)
        {
            return builder.BuildSingleFieldEditor(b.value, new Toggle());
        }

        public static VisualElement BuildStringEditor(this IConstantEditorBuilder builder, ConstantNodeModel<string> s)
        {
            return builder.BuildSingleFieldEditor(s.value, new TextField());
        }

        public static VisualElement BuildCurveEditor(this IConstantEditorBuilder builder, ConstantNodeModel<AnimationCurve> c)
        {
            return builder.BuildSingleFieldEditor(c.value, new CurveField());
        }
    }
}
