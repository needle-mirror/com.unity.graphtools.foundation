using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Plugins.Debugging
{
    public class DebuggingPort : Port
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
            styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>(AssetHelper.AssetPath + "Plugins/Debugging/Elements/Templates/Port.uss"));
        }

        public override bool CanAcceptSelectionDrop(IReadOnlyList<IGraphElementModel> dragSelection)
        {
            return base.CanAcceptSelectionDrop(dragSelection)
                || (dragSelection.Count == 1 && PortModel.PortType != PortType.Execution
                    && (dragSelection.FirstOrDefault() is IVariableDeclarationModel));
        }
    }
}
