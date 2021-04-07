using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.MathBook
{
    public class MathBookBlackboardGraphModel : BlackboardGraphModel
    {
        /// <inheritdoc />
        public MathBookBlackboardGraphModel(IGraphAssetModel graphAssetModel)
            : base(graphAssetModel) { }

        public override void PopulateCreateMenu(string sectionName, GenericMenu menu, CommandDispatcher commandDispatcher)
        {
            menu.AddItem(new GUIContent("Create Variable"), false, () =>
            {
                const string newItemName = "variable";
                var finalName = newItemName;
                var i = 0;
                while (commandDispatcher.GraphToolState.WindowState.GraphModel.VariableDeclarations.Any(v => v.Title == finalName))
                    finalName = newItemName + i++;

                commandDispatcher.Dispatch(new CreateGraphVariableDeclarationCommand(finalName, true, TypeHandle.Float, typeof(MathBookVariableDeclarationModel)));
            });
        }
    }
}
