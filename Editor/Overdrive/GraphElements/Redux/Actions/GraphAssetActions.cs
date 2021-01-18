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
            template?.InitBasicGraph(graphModel);
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

        public static void DefaultReducer(State state, LoadGraphAssetAction action)
        {
            if (ReferenceEquals(Selection.activeObject, state.AssetModel))
                Selection.activeObject = null;

            if (state.GraphModel != null)
            {
                var compilationState = state.CompilationStateComponent;
                // force queued compilation to happen now when unloading a graph
                if (compilationState.CompilationPending)
                {
                    // Do not force compilation if it's the same graph
                    if ((action.AssetPath != null && state.AssetModel.GetPath() != action.AssetPath) ||
                        (action.Asset != null && state.AssetModel != action.Asset))
                    {
                        compilationState.RequestCompilation(RequestCompilationOptions.Default);
                    }
                    compilationState.CompilationPending = false;
                }
            }

            // PF: FIXME: could this be updated by an observer?
            // PF: FIXME: how to notify the owner of the viewGUID that it should update itself?
            switch (action.LoadType)
            {
                case Type.Replace:
                    state.WindowState.ClearHistory();
                    break;
                case Type.PushOnStack:
                    state.WindowState.PushCurrentGraph();
                    break;
                case Type.KeepHistory:
                    break;
            }

            state.AssetModel?.Dispose();

            state.PluginRepository?.UnregisterPlugins();

            var asset = action.Asset;
            if (asset == null)
            {
                // PF FIXME: load the right asset type (not GraphAssetModel)
                asset = (IGraphAssetModel)AssetDatabase.LoadAssetAtPath(action.AssetPath, typeof(GraphAssetModel));
            }

            if (asset == null)
            {
                Debug.LogError($"Could not load visual scripting asset at path '{action.AssetPath}'");
                return;
            }

            var assetPath = AssetDatabase.GetAssetPath(asset as Object);
            AssetWatcher.Instance.WatchGraphAssetAtPath(assetPath, asset);

            state.LoadGraphAsset(asset, action.BoundObject);
            state.BlackboardGraphModel.AssetModel = asset;

            state.RequestUIRebuild();

            var graphModel = state.GraphModel;
            graphModel?.Stencil?.PreProcessGraph(state.GraphModel);

            CheckGraphIntegrity(state);
        }

        static void CheckGraphIntegrity(State state)
        {
            var graphModel = state.GraphModel;
            if (graphModel == null)
                return;

            var invalidNodeCount = graphModel.NodeModels.Count(n => n == null);
            var invalidEdgeCount = graphModel.EdgeModels.Count(n => n == null);
            var invalidStickyCount = state.GraphModel.StickyNoteModels.Count(n => n == null);

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

    public class RequestCompilationAction : BaseAction
    {
        public RequestCompilationOptions Options;

        public RequestCompilationAction(RequestCompilationOptions options)
        {
            UndoString = "Compile";

            Options = options;
        }

        public static void DefaultReducer(State state, RequestCompilationAction action)
        {
            state.CompilationStateComponent.RequestCompilation(action.Options);
        }
    }
}
