using System;
using System.Linq;
using System.Text;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public static class AssetActionHelper
    {
        public static void InitTemplate(IGraphTemplate template, IGraphModel graphModel)
        {
            if (template != null)
            {
                template.InitBasicGraph(graphModel);
                graphModel.LastChanges.ElementsToAutoAlign.AddRange(graphModel.Stencil.GetEntryPoints(graphModel));
            }
        }
    }

    public class LoadGraphAssetAction : BaseAction
    {
        public enum Type
        {
            Replace,
            PushOnStack,
            KeepHistory
        }

        public readonly IGraphAssetModel Asset;
        public readonly string AssetPath;
        public readonly GameObject BoundObject;
        public readonly Type LoadType;

        public LoadGraphAssetAction(string assetPath, GameObject boundObject = null,
                                    Type loadType = Type.Replace)
        {
            Asset = null;
            AssetPath = assetPath;
            BoundObject = boundObject;
            LoadType = loadType;
        }

        public LoadGraphAssetAction(IGraphAssetModel assetModel, GameObject boundObject = null,
                                    Type loadType = Type.Replace)
        {
            AssetPath = null;
            Asset = assetModel;
            BoundObject = boundObject;
            LoadType = loadType;
        }

        public static void DefaultReducer(State previousState, LoadGraphAssetAction action)
        {
            if (ReferenceEquals(Selection.activeObject, previousState.AssetModel))
                Selection.activeObject = null;

            if (previousState.CurrentGraphModel != null)
            {
                // force queued compilation to happen now when unloading a graph
                if (previousState.EditorDataModel?.CompilationPending ?? false)
                {
                    // Do not force compilation if it's the same graph
                    if ((action.AssetPath != null && previousState.CurrentGraphModel.GetAssetPath() != action.AssetPath) ||
                        (action.Asset != null && previousState.AssetModel != action.Asset))
                    {
                        previousState.EditorDataModel.RequestCompilation(RequestCompilationOptions.Default);
                    }
                    previousState.EditorDataModel.CompilationPending = false;
                }
            }

            previousState.AssetModel?.Dispose();
            previousState.EditorDataModel?.PluginRepository?.UnregisterPlugins();

            var asset = action.Asset ?? AssetDatabase.LoadAssetAtPath<GraphAssetModel>(action.AssetPath);
            if (asset == null || !(asset as Object))
            {
                Debug.LogError($"Could not load visual scripting asset at path '{action.AssetPath}'");
                return;
            }

            var assetPath = AssetDatabase.GetAssetPath(asset as Object);
            AssetWatcher.Instance.WatchGraphAssetAtPath(assetPath, asset);

            switch (action.LoadType)
            {
                case Type.Replace:
                    previousState.EditorDataModel?.PreviousGraphModels.Clear();
                    break;
                case Type.PushOnStack:
                    previousState.EditorDataModel?.PreviousGraphModels.Add(new OpenedGraph(previousState.CurrentGraphModel?.AssetModel, previousState.EditorDataModel?.BoundObject));
                    break;
                case Type.KeepHistory:
                    break;
            }

            previousState.AssetModel = asset;

            if (previousState.EditorDataModel != null)
                previousState.EditorDataModel.BoundObject = action.BoundObject;

            previousState.MarkForUpdate(UpdateFlags.All);

            var graphModel = previousState.CurrentGraphModel;
            graphModel?.Stencil?.PreProcessGraph(previousState.CurrentGraphModel);

            CheckGraphIntegrity(previousState);
        }

        static void CheckGraphIntegrity(State state)
        {
            var graphModel = state.CurrentGraphModel;
            if (graphModel == null)
                return;

            var invalidNodeCount = graphModel.NodeModels.Count(n => n == null);
            var invalidEdgeCount = graphModel.EdgeModels.Count(n => n == null);
            var invalidStickyCount = state.CurrentGraphModel.StickyNoteModels.Count(n => n == null);

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
                    graphModel.Repair();
        }
    }
}
