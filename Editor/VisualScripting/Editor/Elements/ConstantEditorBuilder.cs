using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.VisualScripting.Editor.ConstantEditor
{
    class ConstantEditorBuilder : IConstantEditorBuilder
    {
        public Action<IChangeEvent> OnValueChanged { get; }

        public ConstantEditorBuilder(Action<IChangeEvent> onValueChanged)
        {
            OnValueChanged = onValueChanged;
        }

        // Looking for methods like : VisualElement MyFunctionName(IConstantEditorBuilder builder, <NodeTypeToBuild> node)
        public static bool FilterMethods(MethodInfo x)
        {
            var parameters = x.GetParameters();
            return x.ReturnType == typeof(VisualElement)
                && parameters.Length == 2
                && parameters[0].ParameterType == typeof(IConstantEditorBuilder);
        }

        public static Type KeySelector(MethodInfo x)
        {
            return x.GetParameters()[1].ParameterType;
        }
    }
}
