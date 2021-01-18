using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting
{
    public class Port : Overdrive.Port
    {
        public static readonly string portExecutionActiveModifierUssClassName = ussClassName.WithUssModifier("execution-active");

        /// <summary>
        /// Used to highlight the port when it is triggered during tracing
        /// </summary>
        public bool ExecutionPortActive
        {
            set => EnableInClassList(portExecutionActiveModifierUssClassName, value);
        }

        protected override void PostBuildUI()
        {
            base.PostBuildUI();
            styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>(AssetHelper.VSTemplatePath + "Port.uss"));
        }

        public override bool CanAcceptDrop(List<ISelectableGraphElement> dragSelection)
        {
            return base.CanAcceptDrop(dragSelection)
                || (dragSelection.Count == 1 && PortModel.PortType != PortType.Execution
                    && (dragSelection[0] is BlackboardField));
        }
    }
}
