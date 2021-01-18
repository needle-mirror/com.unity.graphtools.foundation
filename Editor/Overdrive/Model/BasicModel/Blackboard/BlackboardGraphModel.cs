using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.BasicModel
{
    public class BlackboardGraphModel : IBlackboardGraphModel
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

        public virtual void PopulateCreateMenu(string sectionName, GenericMenu menu, Store store)
        {
            menu.AddItem(new GUIContent("Create Variable"), false, () =>
            {
                const string newItemName = "variable";
                var finalName = newItemName;
                var i = 0;
                // ReSharper disable once AccessToModifiedClosure
                while (store.State.GraphModel.VariableDeclarations.Any(v => v.Title == finalName))
                    finalName = newItemName + i++;

                store.Dispatch(new CreateGraphVariableDeclarationAction(finalName, true, TypeHandle.Float));
            });
        }

        public IGraphModel GraphModel => AssetModel?.GraphModel;

        public GUID Guid { get; set; }

        public IGraphAssetModel AssetModel { get; set; }

        public void AssignNewGuid()
        {
            Guid = GUID.Generate();
        }

        public IReadOnlyList<Capabilities> Capabilities => new List<Capabilities>
        {
            Overdrive.Capabilities.NoCapabilities
        };
    }
}
