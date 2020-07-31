using System;
using System.Reflection;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.GraphElements
{
    public class ConstantEditorBuilder : IConstantEditorBuilder
    {
        public Action<IChangeEvent> OnValueChanged { get; }
        public IGTFEditorDataModel EditorDataModel { get; }
        public bool ConstantIsLocked { get; }

        public ConstantEditorBuilder(Action<IChangeEvent> onValueChanged, IGTFEditorDataModel editorDataModel,
                                     bool constantIsLocked)
        {
            OnValueChanged = onValueChanged;
            EditorDataModel = editorDataModel;
            ConstantIsLocked = constantIsLocked;
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
