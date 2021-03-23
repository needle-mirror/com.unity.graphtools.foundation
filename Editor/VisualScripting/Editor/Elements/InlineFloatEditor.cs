using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.UIElements;
using UnityEditor.VisualScripting.Model;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.VisualScripting.Editor.ConstantEditor
{
    public interface IVectorType<out T, V>
    {
        T Value { get; }
        IReadOnlyList<string> FieldNames { get; }
        V GetField(int i);
        void SetField(int i, V newValue);
    }

    public static class VectorTypeHelper
    {
        static string s_VectorParamNames = "xyzw";

        public static InlineFloatEditor MakeFloatVectorEditor<T>(this IConstantEditorBuilder builder, ConstantNodeModel<T> model, int vectorSize, Func<T, int, float> getField, VectorType<T, float>.SetFieldDelegate setField)
        {
            var fieldNames = s_VectorParamNames.Take(vectorSize).Select(c => c.ToString());
            return builder.MakeFloatVectorEditor(model, fieldNames, getField, setField);
        }

        public static InlineFloatEditor MakeFloatVectorEditor<T>(this IConstantEditorBuilder builder, ConstantNodeModel<T> model, IEnumerable<string> fieldNames, Func<T, int, float> getField, VectorType<T, float>.SetFieldDelegate setField)
        {
            var vectorType = new VectorType<T, float>(model.value, fieldNames, getField, setField);
            return builder.MakeVectorEditor(vectorType, (label, fieldValue) => new FloatField(label) { value = fieldValue });
        }

        static InlineFloatEditor MakeVectorEditor<T, V, F>(this IConstantEditorBuilder builder, IVectorType<T, V> vectorType, Func<string, V, F> makeField) where F : BaseField<V>
        {
            var editor = new InlineFloatEditor();
            for (int i = 0; i < vectorType.FieldNames.Count; i++)
            {
                var field = makeField(vectorType.FieldNames[i], vectorType.GetField(i));
                editor.Add(field);
                var fieldIndex = i; // apparently safer...
                field.RegisterValueChangedCallback(evt =>
                {
                    var oldValue = vectorType.Value;
                    vectorType.SetField(fieldIndex, evt.newValue);
                    using (ChangeEvent<T> other = ChangeEvent<T>.GetPooled(oldValue, vectorType.Value))
                        builder.OnValueChanged(other);
                });
            }

            return editor;
        }
    }

    public class VectorType<T, V> : IVectorType<T, V>
    {
        public delegate void SetFieldDelegate(ref T v, int i, V newValue);

        public T Value => m_Value;

        public IReadOnlyList<string> FieldNames => m_FieldNames;

        T m_Value;

        List<string> m_FieldNames;

        Func<T, int, V> m_GetField;
        SetFieldDelegate m_SetField;

        public VectorType(T value, IEnumerable<string> fieldNames, Func<T, int, V> getField, SetFieldDelegate setField)
        {
            m_Value = value;
            m_GetField = getField;
            m_SetField = setField;
            m_FieldNames = fieldNames.ToList();
        }

        public V GetField(int i)
        {
            return m_GetField(m_Value, i);
        }

        public void SetField(int i, V newValue)
        {
            m_SetField(ref m_Value, i, newValue);
        }
    }

    public class InlineFloatEditor : VisualElement
    {
        static string GraphToolEditorStylePath => UICreationHelper.templatePath + "ConstantEditors.uss";

        public InlineFloatEditor()
        {
            styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>(GraphToolEditorStylePath));
            AddToClassList("vs-inline-float-editor");
        }
    }
}
