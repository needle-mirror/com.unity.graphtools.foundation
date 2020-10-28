using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting
{
    public class Port : Overdrive.Port
    {
        public static readonly string k_PortExecutionActiveModifer = k_UssClassName.WithUssModifier("execution-active");

        /// <summary>
        /// Used to highlight the port when it is triggered during tracing
        /// </summary>
        public bool ExecutionPortActive
        {
            set => EnableInClassList(k_PortExecutionActiveModifer, value);
        }

        protected override void PostBuildUI()
        {
            base.PostBuildUI();
            styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>(PackageTransitionHelper.VSTemplatePath + "Port.uss"));
        }

        public override bool CanAcceptDrop(List<ISelectableGraphElement> dragSelection)
        {
            return base.CanAcceptDrop(dragSelection)
                || (dragSelection.Count == 1 && PortModel.PortType != PortType.Execution
                    && (dragSelection[0] is IVisualScriptingField || dragSelection[0] is TokenDeclaration));
        }
    }
}
