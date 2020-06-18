using System;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive.GraphElements;
using UnityEditor.Compilation;
using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.Compilation;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting
{
    static class EditorReducers
    {
        public static void Register(Store store)
        {
            store.Register<BuildAllEditorAction>(BuildAllEditor);
            store.Register<AddVisualScriptToObjectAction>(AddVisualScriptToObject);
        }

        static State BuildAllEditor(State previousState, BuildAllEditorAction action)
        {
            BuildAll(action.Callback);

            previousState.MarkForUpdate(UpdateFlags.All | UpdateFlags.CompilationResult);

            return previousState;
        }

        public static void BuildAll(Action<string, CompilerMessage[]> roslynCompilationOnBuildFinished)
        {
            var assetGUIDs = AssetDatabase.FindAssets($"t:{typeof(VSGraphAssetModel).FullName}");

            var assetsByBuilder = assetGUIDs.Select(assetGuid => AssetDatabase.LoadAssetAtPath<VSGraphAssetModel>(AssetDatabase.GUIDToAssetPath(assetGuid)))
                .Where(asset => asset?.GraphModel != null && asset.GraphModel.State == ModelState.Enabled)
                .GroupBy(asset => asset.Builder);
            foreach (IGrouping<IBuilder, VSGraphAssetModel> grouping in assetsByBuilder)
            {
                if (grouping.Key == null)
                    continue;
                var builder = grouping.Key;
                builder.Build(grouping.ToList(), roslynCompilationOnBuildFinished);
            }
        }

        static State AddVisualScriptToObject(State previousState, AddVisualScriptToObjectAction action)
        {
            ((GameObject)action.Instance).AddComponent(action.ComponentType);
//            var component = ((GameObject)action.Instance).GetComponent<MonoBehaviour>();
            throw new NotImplementedException("AddVisualScriptToObject");
//            previousState.MarkForUpdate(UpdateFlags.All);

//            return previousState;
        }
    }
}
