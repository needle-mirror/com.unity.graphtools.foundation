using System;
using System.Linq;
using System.Text;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;
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

    public class LoadGraphAssetCommand : UndoableCommand
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
        public readonly long FileId;
        public readonly PluginRepository PluginRepository;
        public readonly int TruncateHistoryIndex;

        public LoadGraphAssetCommand(string assetPath, PluginRepository pluginRepository, GameObject boundObject = null,
                                     Type loadType = Type.Replace, long filedId = 0L, int truncateHistoryIndex = -1)
        {
            Asset = null;
            AssetPath = assetPath;
            BoundObject = boundObject;
            LoadType = loadType;
            FileId = filedId;
            PluginRepository = pluginRepository;
            TruncateHistoryIndex = truncateHistoryIndex;
        }

        public LoadGraphAssetCommand(IGraphAssetModel assetModel, GameObject boundObject = null,
                                     Type loadType = Type.Replace)
        {
            AssetPath = null;
            Asset = assetModel;
            BoundObject = boundObject;
            LoadType = loadType;
            TruncateHistoryIndex = -1;
        }

        public static void DefaultCommandHandler(GraphToolState graphToolState, LoadGraphAssetCommand command)
        {
            if (ReferenceEquals(Selection.activeObject, graphToolState.WindowState.AssetModel))
                Selection.activeObject = null;

            if (graphToolState.WindowState.GraphModel != null)
            {
                var graphProcessingStateComponent = graphToolState.GraphProcessingState;
                // force queued graph processing to happen now when unloading a graph
                if (graphProcessingStateComponent.GraphProcessingPending)
                {
                    // Do not force graph processing if it's the same graph
                    if ((command.AssetPath != null && graphToolState.WindowState.AssetModel.GetPath() != command.AssetPath) ||
                        (command.Asset != null && graphToolState.WindowState.AssetModel != command.Asset))
                    {
                        GraphProcessingHelper.ProcessGraph(graphToolState.WindowState.GraphModel, command.PluginRepository,
                            RequestGraphProcessingOptions.Default, graphToolState.TracingStatusState.TracingEnabled);
                    }

                    using (var graphProcessingStateUpdater = graphToolState.GraphProcessingState.UpdateScope)
                    {
                        graphProcessingStateUpdater.GraphProcessingPending = false;
                    }
                }
            }

            using (var windowStateUpdater = graphToolState.WindowState.UpdateScope)
            {
                if (command.TruncateHistoryIndex >= 0)
                    windowStateUpdater.TruncateHistory(command.TruncateHistoryIndex);

                switch (command.LoadType)
                {
                    case Type.Replace:
                        windowStateUpdater.ClearHistory();
                        break;
                    case Type.PushOnStack:
                        windowStateUpdater.PushCurrentGraph();
                        break;
                    case Type.KeepHistory:
                        break;
                }

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

                var graphModel = graphToolState.WindowState.GraphModel;
                ((Stencil)graphModel?.Stencil)?.PreProcessGraph(graphModel);

                CheckGraphIntegrity(graphToolState);
            }
        }

        static void CheckGraphIntegrity(GraphToolState graphToolState)
        {
            var graphModel = graphToolState.WindowState.GraphModel;
            if (graphModel == null)
                return;

            var invalidNodeCount = graphModel.NodeModels.Count(n => n == null);
            var invalidEdgeCount = graphModel.EdgeModels.Count(n => n == null);
            var invalidStickyCount = graphToolState.WindowState.GraphModel.StickyNoteModels.Count(n => n == null);

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
