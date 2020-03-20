using System;
using System.IO;
using System.Linq;
using UnityEditor.VisualScripting.Editor.SmartSearch;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model;
using UnityEditor.VisualScripting.Model.Stencils;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityEditor.VisualScripting.Editor
{
    static class NodeReducers
    {
        public static void Register(Store store)
        {
            store.Register<DisconnectNodeAction>(DisconnectNode);
            store.Register<CreateNodeAction>(CreateNode);
            store.Register<SetNodeEnabledStateAction>(SetNodeEnabledState);
            store.Register<RefactorConvertToFunctionAction>(RefactorConvertToFunction);
            store.Register<RefactorExtractMacroAction>(RefactorExtractMacro);
            store.Register<RefactorExtractFunctionAction>(RefactorExtractFunction);
            store.Register<CreateMacroRefAction>(CreateMacroRefNode);
        }

        static State CreateNode(State previousState, CreateNodeAction action)
        {
            var nodes = action.CreateElements.Invoke(
                new GraphNodeCreationData(action.GraphModel, action.Position, guids: action.Guids));

            if (nodes.Any(n => n is EdgeModel))
                previousState.CurrentGraphModel.LastChanges.ModelsToAutoAlign.AddRange(nodes);

            previousState.MarkForUpdate(UpdateFlags.GraphTopology);
            return previousState;
        }

        static State DisconnectNode(State previousState, DisconnectNodeAction action)
        {
            var graphModel = (VSGraphModel)previousState.CurrentGraphModel;

            foreach (INodeModel nodeModel in action.NodeModels)
            {
                var edgeModels = graphModel.GetEdgesConnections(nodeModel);

                graphModel.DeleteEdges(edgeModels);
            }

            return previousState;
        }

        static State SetNodeEnabledState(State previousState, SetNodeEnabledStateAction action)
        {
            Undo.RegisterCompleteObjectUndo((Object)previousState.AssetModel, action.State == ModelState.Enabled ? "Enable Nodes" : "Disable Nodes");
            EditorUtility.SetDirty((Object)previousState.AssetModel);
            foreach (NodeModel nodeModel in action.NodeToConvert)
                nodeModel.State = action.State;
            previousState.MarkForUpdate(UpdateFlags.GraphTopology);
            return previousState;
        }

        static State RefactorConvertToFunction(State previousState, RefactorConvertToFunctionAction action)
        {
            var graphModel = (VSGraphModel)previousState.CurrentGraphModel;
            var newFunction = graphModel.ConvertNodeToFunction(action.NodeToConvert);
            previousState.EditorDataModel.ElementModelToRename = newFunction;
            previousState.MarkForUpdate(UpdateFlags.GraphTopology);
            return previousState;
        }

        static State RefactorExtractFunction(State previousState, RefactorExtractFunctionAction action)
        {
            var graphModel = (VSGraphModel)previousState.CurrentGraphModel;
            var newFunction = graphModel.ExtractNodesAsFunction(action.Selection);
            previousState.EditorDataModel.ElementModelToRename = newFunction;
            previousState.MarkForUpdate(UpdateFlags.GraphTopology);
            return previousState;
        }

        static State RefactorExtractMacro(State previousState, RefactorExtractMacroAction action)
        {
            var graphModel = (VSGraphModel)previousState.CurrentGraphModel;
            INodeModel newMacro;

            using (new AssetWatcher.Scope())
            {
                var assetName = string.IsNullOrEmpty(action.MacroPath)
                    ? null
                    : Path.GetFileNameWithoutExtension(action.MacroPath);
                var macroGraphAssetModel = (VSGraphAssetModel)GraphAssetModel.Create(
                    assetName, action.MacroPath, typeof(VSGraphAssetModel));
                var macroGraph = macroGraphAssetModel.CreateVSGraph<MacroStencil>(assetName);

                // A MacroStencil cannot be a parent stencil, so use its parent instead
                var parentGraph = graphModel.Stencil is MacroStencil macroStencil
                    ? macroStencil.ParentType
                    : graphModel.Stencil.GetType();

                ((MacroStencil)macroGraph.Stencil).SetParent(parentGraph, macroGraph);
                Utility.SaveAssetIntoObject((Object)macroGraph.AssetModel, macroGraphAssetModel);
                newMacro = graphModel.ExtractNodesAsMacro(macroGraph, action.Position, action.Selection);
                AssetDatabase.SaveAssets();
            }
            previousState.EditorDataModel.ElementModelToRename = newMacro;
            previousState.MarkForUpdate(UpdateFlags.GraphTopology);
            return previousState;
        }

        static State CreateMacroRefNode(State previousState, CreateMacroRefAction action)
        {
            ((VSGraphModel)previousState.CurrentGraphModel).CreateMacroRefNode((VSGraphModel)action.GraphModel, action.Position);

            previousState.MarkForUpdate(UpdateFlags.GraphTopology /*, createdModel*/); // TODO support in partial rebuild
            return previousState;
        }
    }
}
