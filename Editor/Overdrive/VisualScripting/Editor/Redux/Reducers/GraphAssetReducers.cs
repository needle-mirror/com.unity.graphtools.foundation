using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting
{
    static class GraphAssetReducers
    {
        public static void Register(Store store)
        {
            store.RegisterReducer<State, CreateGraphAssetAction>(CreateGraphAsset);
            store.RegisterReducer<State, CreateGraphAssetFromModelAction>(CreateGraphAssetFromModel);
            store.RegisterReducer<State, LoadGraphAssetAction>(LoadGraphAsset);
            store.RegisterReducer<State, UnloadGraphAssetAction>(UnloadGraphAsset);
        }

        static State CreateGraphAssetFromModel(State previousState, CreateGraphAssetFromModelAction action)
        {
            previousState.AssetModel?.Dispose();
            using (new AssetWatcher.Scope())
            {
                AssetDatabase.CreateAsset(
                    action.AssetModel as Object,
                    AssetDatabase.GenerateUniqueAssetPath(action.Path));

                action.AssetModel.CreateGraph(
                    Path.GetFileNameWithoutExtension(action.Path),
                    action.GraphTemplate.StencilType);

                InitTemplate(action.GraphTemplate, action.AssetModel.GraphModel);

                previousState.AssetModel = action.AssetModel;
            }

            SaveAssetAndMarkForUpdate(previousState, true, action.Path);

            return previousState;
        }

        static State CreateGraphAsset(State previousState, CreateGraphAssetAction action)
        {
            previousState.AssetModel?.Dispose();
            using (new AssetWatcher.Scope())
            {
                var graphAssetModel = GraphAssetModel.Create(
                    action.Name,
                    action.AssetPath,
                    action.AssetType,
                    action.WriteOnDisk);
                graphAssetModel.CreateGraph(
                    action.Name,
                    action.StencilType,
                    action.WriteOnDisk);

                InitTemplate(action.GraphTemplate, graphAssetModel.GraphModel);

                previousState.AssetModel = graphAssetModel;

                if (action.Instance)
                    previousState.EditorDataModel.BoundObject = action.Instance;
            }

            SaveAssetAndMarkForUpdate(previousState, action.WriteOnDisk, action.AssetPath);

            return previousState;
        }

        static void InitTemplate(IGraphTemplate template, IGTFGraphModel graphModel)
        {
            if (template != null)
            {
                template.InitBasicGraph(graphModel);
                graphModel.LastChanges.ElementsToAutoAlign.AddRange(graphModel.Stencil.GetEntryPoints(graphModel));
            }
        }

        static void SaveAssetAndMarkForUpdate(State previousState, bool writeOnDisk, string path)
        {
            if (writeOnDisk)
                AssetDatabase.SaveAssets();

            AssetWatcher.Instance.WatchGraphAssetAtPath(path, (GraphAssetModel)previousState.AssetModel);
            previousState.MarkForUpdate(UpdateFlags.All);
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

        static State LoadGraphAsset(State previousState, LoadGraphAssetAction action)
        {
            if (ReferenceEquals(Selection.activeObject, previousState.AssetModel))
                Selection.activeObject = null;
            if (previousState.CurrentGraphModel != null)
            {
                // force queued compilation to happen now when unloading a graph
                if (previousState.EditorDataModel?.CompilationPending ?? false)
                {
                    // Do not force compilation if it's the same graph
                    if (((GraphModel)previousState.CurrentGraphModel).GetAssetPath() !=  action.AssetPath)
                        previousState.EditorDataModel.RequestCompilation(RequestCompilationOptions.Default);
                    previousState.EditorDataModel.CompilationPending = false;
                }
            }
            previousState.AssetModel?.Dispose();
            previousState.EditorDataModel?.PluginRepository?.UnregisterPlugins();

            var asset = AssetDatabase.LoadAssetAtPath<GraphAssetModel>(action.AssetPath);
            if (!asset)
            {
                Debug.LogError($"Could not load visual scripting asset at path '{action.AssetPath}'");
                return previousState;
            }
            AssetWatcher.Instance.WatchGraphAssetAtPath(action.AssetPath, asset);

            switch (action.LoadType)
            {
                case LoadGraphAssetAction.Type.Replace:
                    previousState.EditorDataModel?.PreviousGraphModels.Clear();
                    break;
                case LoadGraphAssetAction.Type.PushOnStack:
                    previousState.EditorDataModel?.PreviousGraphModels.Add(new OpenedGraph(previousState.CurrentGraphModel?.AssetModel, previousState.EditorDataModel?.BoundObject));
                    break;
                case LoadGraphAssetAction.Type.KeepHistory:
                    break;
            }

            previousState.AssetModel = asset;

            if (previousState.EditorDataModel != null)
                previousState.EditorDataModel.BoundObject = action.BoundObject;

            previousState.MarkForUpdate(UpdateFlags.All);

            var graphModel = previousState.CurrentGraphModel;
            if (graphModel?.Stencil != null)
            {
                graphModel.Stencil.PreProcessGraph(previousState.CurrentGraphModel);
                if (action.AlignAfterLoad)
                    graphModel.LastChanges.ElementsToAutoAlign.AddRange(graphModel.Stencil.GetEntryPoints(graphModel));
            }

            CheckGraphIntegrity(previousState);

            return previousState;
        }

        static State UnloadGraphAsset(State previousState, UnloadGraphAssetAction action)
        {
            if (previousState.CurrentGraphModel != null)
                AssetWatcher.Instance.UnwatchGraphAssetAtPath(previousState.CurrentGraphModel.GetAssetPath());
            previousState.UnloadCurrentGraphAsset();
            previousState.MarkForUpdate(UpdateFlags.All);

            return previousState;
        }
    }
}
