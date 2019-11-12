using System;
using System.Linq;
using System.Text;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model;
using UnityEngine;

namespace UnityEditor.VisualScripting.Editor
{
    static class GraphAssetReducers
    {
        public static void Register(Store store)
        {
            store.Register<CreateGraphAssetAction>(CreateGraphAsset);
            store.Register<LoadGraphAssetAction>(LoadGraphAsset);
            store.Register<UnloadGraphAssetAction>(UnloadGraphAsset);
        }

        static State CreateGraphAsset(State previousState, CreateGraphAssetAction action)
        {
            previousState.AssetModel?.Dispose();
            using (new AssetWatcher.Scope())
            {
                GraphAssetModel graphAssetModel = GraphAssetModel.Create(action.Name, action.AssetPath, action.AssetType, action.WriteOnDisk);

                var graphModel = graphAssetModel.CreateGraph(action.GraphType, action.Name, action.StencilType, action.WriteOnDisk);
                if (action.GraphTemplate != null)
                {
                    action.GraphTemplate.InitBasicGraph(graphModel as VSGraphModel);
                    previousState.requestNodeAlignment = true;
                }

                previousState.AssetModel = graphAssetModel;
                if (action.Instance)
                    previousState.EditorDataModel.BoundObject = action.Instance;
            }
            if (action.WriteOnDisk)
                AssetDatabase.SaveAssets();

            previousState.MarkForUpdate(UpdateFlags.All);

            return previousState;
        }

        static void CheckGraphIntegrity(State state)
        {
            var graphModel = state.CurrentGraphModel;
            if (graphModel == null)
                return;

            var invalidNodeCount = graphModel.NodeModels.Count(n => n == null);
            var invalidEdgeCount = graphModel.EdgeModels.Count(n => n == null);
            var invalidStickyCount = ((VSGraphModel)state.CurrentGraphModel).StickyNoteModels.Count(n => n == null);

            var countMessage = new StringBuilder();
            countMessage.Append(invalidNodeCount == 0 ? string.Empty : $"{invalidNodeCount} invalid node(s) found.\n");
            countMessage.Append(invalidEdgeCount == 0 ? string.Empty : $"{invalidEdgeCount} invalid edge(s) found.\n");
            countMessage.Append(invalidStickyCount == 0 ? string.Empty : $"{invalidStickyCount} invalid sticky note(s) found.\n");

            if (countMessage.ToString() != string.Empty)
                if (EditorUtility.DisplayDialog("Invalid graph",
                    $"Invalid elements found:\n{countMessage}\n" +
                    $"Click the Clean button to remove all the invalid elements from the graph.",
                    "Clean",
                    "Cancel"))
                    graphModel.CleanUp();

            foreach (var node in graphModel.NodeModels.Where(n => n is IStackModel).Cast<IStackModel>())
            {
                CheckStackIntegrity(node);
            }
        }

        static void CheckStackIntegrity(IStackModel stackModel)
        {
            var name = string.IsNullOrEmpty(stackModel.Title) ? "Unnamed" : stackModel.Title;
            var invalidNodeCount = stackModel.NodeModels.Count(n => n == null);
            if (invalidNodeCount > 0)
                if (EditorUtility.DisplayDialog("Invalid stack",
                    $"{invalidNodeCount} invalid elements found in stack {name}.\n" +
                    $"Click the Clean button to remove all the invalid elements from the {name} stack.",
                    "Clean",
                    "Cancel"))
                    stackModel.CleanUp();
        }

        static State LoadGraphAsset(State previousState, LoadGraphAssetAction action)
        {
            if (ReferenceEquals(Selection.activeObject, previousState.AssetModel))
                Selection.activeObject = null;
            previousState.AssetModel?.Dispose();
            previousState.EditorDataModel.PluginRepository?.UnregisterPlugins();

            var asset = AssetDatabase.LoadAssetAtPath<GraphAssetModel>(action.AssetPath);
            if (!asset)
            {
                Debug.LogError($"Could not load visual scripting asset at path '{action.AssetPath}'");
                return previousState;
            }

            switch (action.LoadType)
            {
                case LoadGraphAssetAction.Type.Replace:
                    previousState.EditorDataModel.PreviousGraphModels.Clear();
                    break;
                case LoadGraphAssetAction.Type.PushOnStack:
                    previousState.EditorDataModel.PreviousGraphModels.Add((GraphModel)previousState.CurrentGraphModel);
                    break;
                case LoadGraphAssetAction.Type.KeepHistory:
                    break;
            }

            previousState.AssetModel = asset;
            previousState.MarkForUpdate(UpdateFlags.All);

            if (previousState.CurrentGraphModel?.Stencil != null)
                previousState.CurrentGraphModel.Stencil.PreProcessGraph((VSGraphModel)previousState.CurrentGraphModel);

            previousState.requestNodeAlignment = action.AlignAfterLoad;

            CheckGraphIntegrity(previousState);

            return previousState;
        }

        static State UnloadGraphAsset(State previousState, UnloadGraphAssetAction action)
        {
            previousState.UnloadCurrentGraphAsset();
            previousState.MarkForUpdate(UpdateFlags.All);

            return previousState;
        }
    }
}
