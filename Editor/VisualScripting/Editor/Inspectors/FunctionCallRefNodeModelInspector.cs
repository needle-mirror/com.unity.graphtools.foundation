using System;
using System.Linq;
using UnityEditor.VisualScripting.Model;

namespace UnityEditor.VisualScripting.Editor
{
    [CustomEditor(typeof(FunctionRefCallNodeModel))]
    class FunctionCallRefNodeModelInspector : NodeModelInspector
    {
        protected override bool DoDefaultInspector => false;

        protected override void GraphElementInspectorGUI(Action refreshUI)
        {
//            if (!(target is NodeAsset<FunctionRefCallNodeModel> asset))
//                return;
//
//            var decl = asset.Node;
//            var assetGUIDs = AssetDatabase.FindAssets($"t:{typeof(VSGraphAssetModel).FullName}");
//
//            var functions = assetGUIDs.SelectMany(assetGuid =>
//            {
//                var assetPath = AssetDatabase.GUIDToAssetPath(assetGuid);
//                var graphAssetModel = AssetDatabase.LoadAssetAtPath<VSGraphAssetModel>(assetPath);
//
//                return graphAssetModel.GraphModel?.NodeModels.OfType<FunctionModel>() ?? Enumerable.Empty<FunctionModel>();
//            }).ToArray();
//
//            int current = Array.IndexOf(functions, decl.Function);
//            var functionNames = functions.Select(f => $"{f.GraphModel.Name} > {f.Title}").ToArray();
//            int newFunction = EditorGUILayout.Popup("Function", current, functionNames);
//            if (current != newFunction)
//                decl.Function = functions[newFunction];
//            DisplayPorts(decl);
        }
    }
}
