using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.BasicModel
{
    /// <summary>
    /// A model that represents a blackboard for a graph.
    /// </summary>
    public class BlackboardGraphModel : GraphElementModel, IBlackboardGraphModel
    {
        public bool Valid => GraphModel != null;

        public virtual string GetBlackboardTitle()
        {
            return AssetModel?.FriendlyScriptName ?? "";
        }

        public virtual string GetBlackboardSubTitle()
        {
            return "Class Library";
        }

        public virtual IEnumerable<string> SectionNames =>
            GraphModel == null ? Enumerable.Empty<string>() : new List<string>() { "Graph Variables" };

        public virtual IEnumerable<IVariableDeclarationModel> GetSectionRows(string sectionName)
        {
            return GraphModel?.VariableDeclarations ?? Enumerable.Empty<IVariableDeclarationModel>();
        }

        public virtual void PopulateCreateMenu(string sectionName, GenericMenu menu, CommandDispatcher commandDispatcher)
        {
            menu.AddItem(new GUIContent("Create Variable"), false, () =>
            {
                const string newItemName = "variable";
                var finalName = newItemName;
                var i = 0;
                // ReSharper disable once AccessToModifiedClosure
                while (commandDispatcher.GraphToolState.WindowState.GraphModel.VariableDeclarations.Any(v => v.Title == finalName))
                    finalName = newItemName + i++;

                commandDispatcher.Dispatch(new CreateGraphVariableDeclarationCommand(finalName, true, TypeHandle.Float));
            });
        }
    }
}
