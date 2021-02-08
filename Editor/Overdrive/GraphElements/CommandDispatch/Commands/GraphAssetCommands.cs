using System;
using System.Linq;
using System.Text;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public static class AssetActionHelper
    {
        public static void InitTemplate(IGraphTemplate template, IGraphModel graphModel)
        {
            template?.InitBasicGraph(graphModel);
        }
    }

    public class LoadGraphAssetCommand : Command
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
        public readonly long FileId = 0L;

        public LoadGraphAssetCommand(string assetPath, GameObject boundObject = null,
                                     Type loadType = Type.Replace, long filedId = 0L)
        {
            Asset = null;
            AssetPath = assetPath;
            BoundObject = boundObject;
            LoadType = loadType;
            FileId = filedId;
        }

        public LoadGraphAssetCommand(IGraphAssetModel assetModel, GameObject boundObject = null,
                                     Type loadType = Type.Replace)
        {
            AssetPath = null;
            Asset = assetModel;
            BoundObject = boundObject;
            LoadType = loadType;
        }

        public static void DefaultCommandHandler(GraphToolState graphToolState, LoadGraphAssetCommand command)
        {
            if (ReferenceEquals(Selection.activeObject, graphToolState.AssetModel))
                Selection.activeObject = null;

            if (graphToolState.GraphModel != null)
            {
                var graphProcessingStateComponent = graphToolState.GraphProcessingStateComponent;
                // force queued graph processing to happen now when unloading a graph
                if (graphProcessingStateComponent.GraphProcessingPending)
                {
                    // Do not force graph processing if it's the same graph
                    if ((command.AssetPath != null && graphToolState.AssetModel.GetPath() != command.AssetPath) ||
                        (command.Asset != null && graphToolState.AssetModel != command.Asset))
                    {
                        graphProcessingStateComponent.RequestGraphProcessing(RequestGraphProcessingOptions.Default);
                    }
                    graphProcessingStateComponent.GraphProcessingPending = false;
                }
            }

            // PF: FIXME: could this be updated by an observer?
            // PF: FIXME: how to notify the owner of the viewGUID that it should update itself?
            switch (command.LoadType)
            {
                case Type.Replace:
                    graphToolState.WindowState.ClearHistory();
                    break;
                case Type.PushOnStack:
                    graphToolState.WindowState.PushCurrentGraph();
                    break;
                case Type.KeepHistory:
                    break;
            }

            graphToolState.AssetModel?.Dispose();

            var asset = command.Asset;
            if (asset == null)
            {
                if (command.FileId != 0L)
                {
                    var assets = AssetDatabase.LoadAllAssetsAtPath(command.AssetPath);
                    foreach (var a in assets)
                    {
                        if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(a, out _, out long localId))
                            continue;
                        if (localId == command.FileId)
                        {
                            asset = a as IGraphAssetModel;
                            break;
                        }
                    }
                }
                else
                {
                    // PF FIXME: load the right asset type (not GraphAssetModel)
                    asset = (IGraphAssetModel)AssetDatabase.LoadAssetAtPath(command.AssetPath, typeof(GraphAssetModel));
                }
            }

            if (asset == null)
            {
                Debug.LogError($"Could not load visual scripting asset at path '{command.AssetPath}'");
                return;
            }

            graphToolState.LoadGraphAsset(asset, command.BoundObject);
            graphToolState.BlackboardGraphModel.AssetModel = asset;

            graphToolState.RequestUIRebuild();

            var graphModel = graphToolState.GraphModel;
            graphModel?.Stencil?.PreProcessGraph(graphToolState.GraphModel);

            CheckGraphIntegrity(graphToolState);
        }

        static void CheckGraphIntegrity(GraphToolState graphToolState)
        {
            var graphModel = graphToolState.GraphModel;
            if (graphModel == null)
                return;

            var invalidNodeCount = graphModel.NodeModels.Count(n => n == null);
            var invalidEdgeCount = graphModel.EdgeModels.Count(n => n == null);
            var invalidStickyCount = graphToolState.GraphModel.StickyNoteModels.Count(n => n == null);

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
