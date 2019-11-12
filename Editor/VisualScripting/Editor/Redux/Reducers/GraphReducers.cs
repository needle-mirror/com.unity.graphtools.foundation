using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.EditorCommon.Utility;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityEditor.VisualScripting.Editor
{
    static class GraphReducers
    {
        public static void Register(Store store)
        {
            store.Register<CreateFunctionAction>(CreateFunction);
            store.Register<CreateEventFunctionAction>(CreateEventFunction);
            store.Register<RenameElementAction>(RenameElement);
            store.Register<DeleteElementsAction>(DeleteElements);
            store.Register<RemoveNodesAction>(BypassAndDeleteElements);
            store.Register<PasteSerializedDataAction>(PasteSerializedData);
            store.Register<MoveElementsAction>(MoveElements);
            store.Register<PanToNodeAction>(PanToNode);
        }

        static State PasteSerializedData(State previousState, PasteSerializedDataAction action)
        {
            VseGraphView.OnUnserializeAndPaste(action.Graph, action.Info, action.EditorDataModel, action.Data);
            return previousState;
        }

        static State DeleteElements(State previousState, DeleteElementsAction action)
        {
            var graphModel = (GraphModel)previousState.CurrentGraphModel;

            Undo.RegisterCompleteObjectUndo((Object)graphModel.AssetModel, "Delete elements");
            var elementsToRemove = action.ElementsToRemove;

            DeleteElementsFromGraph(previousState, elementsToRemove, graphModel);

            return previousState;
        }

        static void DeleteElementsFromGraph(State previousState, IReadOnlyCollection<IGraphElementModel> elementsToRemove, GraphModel graphModel)
        {
            IGraphElementModel[] deletables = elementsToRemove.Where(x => (x.Capabilities & CapabilityFlags.Deletable) != 0).Distinct().ToArray();

            var vsGraphModel = (VSGraphModel)graphModel;
            IStickyNoteModel[] stickyNotesToDelete = GetStickyNotesToDelete(deletables);
            IReadOnlyCollection<INodeModel> nodesToDelete = GetNodesToDelete(vsGraphModel, deletables);
            IReadOnlyCollection<IEdgeModel> edgesToDelete = GetEdgesToDelete(graphModel, deletables, nodesToDelete);
            VariableDeclarationModel[] declarationModelsToDelete = GetDeclarationModelsToDelete(deletables, nodesToDelete);

            if (declarationModelsToDelete.Any())
                previousState.MarkForUpdate(UpdateFlags.RequestRebuild);
            if (IsReferenceDatabaseDirty(nodesToDelete))
                graphModel.Stencil.GetSearcherDatabaseProvider().ClearReferenceItemsSearcherDatabases();
            graphModel.DeleteStickyNotes(stickyNotesToDelete);
            graphModel.DeleteEdges(edgesToDelete);
            graphModel.DeleteNodes(nodesToDelete, GraphModel.DeleteConnections.False);
            vsGraphModel.DeleteVariableDeclarations(declarationModelsToDelete, false);
        }

        static VariableDeclarationModel[] GetDeclarationModelsToDelete(IGraphElementModel[] deletables, IReadOnlyCollection<INodeModel> nodesToDelete)
        {
            return deletables.OfType<VariableDeclarationModel>().Where(x => !nodesToDelete.Contains(x.FunctionModel)).ToArray();
        }

        static IStickyNoteModel[] GetStickyNotesToDelete(IGraphElementModel[] deletables)
        {
            return deletables.OfType<IStickyNoteModel>().ToArray();
        }

        static bool IsReferenceDatabaseDirty(IReadOnlyCollection<INodeModel> nodesToDelete)
        {
            return nodesToDelete.OfType<IFunctionModel>().Any();
        }

        static List<IEdgeModel> GetEdgesToDelete(GraphModel graphModel, IGraphElementModel[] deletables, IReadOnlyCollection<INodeModel> nodesToDelete)
        {
            var edgesToDelete = new HashSet<IEdgeModel>(deletables.OfType<IEdgeModel>());
            foreach (var node in nodesToDelete.Where(n => n.ParentStackModel == null || !n.ParentStackModel.DelegatesOutputsToNode(out var lastNode) || !ReferenceEquals(lastNode, n)))
                foreach (var portModel in node.GetPortModels())
                    edgesToDelete.AddRange(graphModel.EdgesConnectedToPorts(portModel));
            return edgesToDelete.ToList();
        }

        static List<INodeModel> GetNodesToDelete(VSGraphModel vsGraphModel, IGraphElementModel[] deletables)
        {
            var nodesToDelete = new HashSet<INodeModel>(deletables.OfType<NodeModel>());
            nodesToDelete.AddRange(deletables.OfType<IStackModel>().SelectMany(s => s.NodeModels));
            nodesToDelete.AddRange(deletables.OfType<VariableDeclarationModel>().SelectMany(vsGraphModel.FindUsages));
            return nodesToDelete.ToList();
        }

        static State BypassAndDeleteElements(State previousState, RemoveNodesAction action)
        {
            var graphModel = (VSGraphModel)previousState.CurrentGraphModel;

            Undo.RegisterCompleteObjectUndo((Object)graphModel.AssetModel, "Delete elements");

            graphModel.BypassNodes(action.NodesToBypass);
            List<IGraphElementModel> graphElementsToRemove = action.ElementsToRemove.Cast<IGraphElementModel>().ToList();
            DeleteElementsFromGraph(previousState, graphElementsToRemove, graphModel);
            return previousState;
        }

        static State CreateFunction(State previousState, CreateFunctionAction action)
        {
            VSGraphModel graphModel = (VSGraphModel)previousState.CurrentGraphModel;

            FunctionModel functionModel = graphModel.CreateFunction(action.Name, action.Position);
            previousState.EditorDataModel.ElementModelToRename = functionModel;
            previousState.MarkForUpdate(UpdateFlags.RequestRebuild);

            return previousState;
        }

        static State CreateEventFunction(State previousState, CreateEventFunctionAction action)
        {
            VSGraphModel graphModel = (VSGraphModel)previousState.CurrentGraphModel;
            graphModel.CreateEventFunction(action.MethodInfo, action.Position);
            previousState.MarkForUpdate(UpdateFlags.RequestRebuild);

            return previousState;
        }

        static State RenameElement(State previousState, RenameElementAction action)
        {
            if (string.IsNullOrWhiteSpace(action.Name))
                return previousState;

            Undo.RegisterCompleteObjectUndo((Object)action.RenamableModel.AssetModel, "Renaming graph element");
            action.RenamableModel.Rename(action.Name);

            IGraphChangeList graphChangeList = previousState.CurrentGraphModel.LastChanges;

            VSGraphModel vsGraphModel = (VSGraphModel)previousState.CurrentGraphModel;

            if (action.RenamableModel is IFunctionModel functionDefinitionModel)
            {
                RenameFunctionUsages((FunctionModel)functionDefinitionModel);
            }
            else if (action.RenamableModel is VariableDeclarationModel variableDeclarationModel)
            {
                graphChangeList.BlackBoardChanged = true;

                if (variableDeclarationModel.Owner is IFunctionModel functionModel)
                {
                    RenameFunctionUsages((FunctionModel)functionModel);
                }

                // update usage names
                graphChangeList.ChangedElements.AddRange(vsGraphModel.FindUsages(variableDeclarationModel));
            }
            else if (action.RenamableModel is IVariableModel variableModel)
            {
                graphChangeList.BlackBoardChanged = true;

                variableDeclarationModel = variableModel.DeclarationModel as VariableDeclarationModel;
                graphChangeList.ChangedElements.Add(variableDeclarationModel);

                graphChangeList.ChangedElements.AddRange(vsGraphModel.FindUsages(variableDeclarationModel));
                if (variableDeclarationModel.FunctionModel != null)
                    graphChangeList.ChangedElements.Add(variableDeclarationModel.FunctionModel);
            }
            else
                graphChangeList.ChangedElements.Add(action.RenamableModel);


            previousState.MarkForUpdate(UpdateFlags.RequestCompilation | UpdateFlags.RequestRebuild);

            return previousState;

            void RenameFunctionUsages(FunctionModel functionModel)
            {
                var toUpdate = functionModel.FindFunctionUsages(previousState.CurrentGraphModel);
                graphChangeList.ChangedElements.AddRange(toUpdate);
                graphChangeList.ChangedElements.Add(functionModel);
            }
        }

        static State MoveElements(State previousState, MoveElementsAction action)
        {
            Undo.RegisterCompleteObjectUndo((Object)previousState.AssetModel, "Move");

            // TODO It would be nice to have a single way of moving things around and thus not having to deal with 3
            // separate collections.
            if (action.NodeModels != null)
                foreach (var nodeModel in action.NodeModels)
                    ((GraphModel)previousState.CurrentGraphModel).MoveNode(nodeModel, nodeModel.Position + action.Delta);

            if (action.StickyModels != null)
            {
                foreach (var stickyNoteModel in action.StickyModels)
                    stickyNoteModel.Move(new Rect(stickyNoteModel.Position.position + action.Delta, stickyNoteModel.Position.size));
            }

            previousState.MarkForUpdate(UpdateFlags.GraphGeometry);
            return previousState;
        }

        static State PanToNode(State previousState, PanToNodeAction action)
        {
            previousState.EditorDataModel.NodeToFrameGuid = action.nodeGuid;
            previousState.MarkForUpdate(UpdateFlags.GraphGeometry);
            return previousState;
        }
    }
}
