using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public class ConstantEditorBuilder : IConstantEditorBuilder
    {
        public Action<IChangeEvent> OnValueChanged { get; }
        public Store Store { get; }
        public bool ConstantIsLocked { get; }
        public IPortModel PortModel { get; }

        public ConstantEditorBuilder(Action<IChangeEvent> onValueChanged,
                                     Store store,
                                     bool constantIsLocked, IPortModel portModel)
        {
            OnValueChanged = onValueChanged;
            Store = store;
            ConstantIsLocked = constantIsLocked;
            PortModel = portModel;
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
