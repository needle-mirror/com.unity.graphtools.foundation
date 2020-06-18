using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive.GraphElements;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.GraphViewModel;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting
{
    static class GraphReducers
    {
        public static void Register(Store store)
        {
            store.Register<RenameElementAction>(RenameElement);
            store.Register<DeleteElementsAction>(DeleteElements);
            store.Register<RemoveNodesAction>(BypassAndDeleteElements);
            store.Register<PasteSerializedDataAction>(PasteSerializedData);
            store.Register<MoveElementsAction>(MoveElements);
            store.Register<PanToNodeAction>(PanToNode);
            store.Register<ChangeElementColorAction>(ChangeElementColor);
            store.Register<ResetElementColorAction>(ResetElementColor);
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

        static void DeleteElementsFromGraph(State previousState, IReadOnlyCollection<IGTFGraphElementModel> elementsToRemove, GraphModel graphModel)
        {
            IGTFGraphElementModel[] deletables = elementsToRemove.Where(x => x is IDeletable).Distinct().ToArray();

            var vsGraphModel = (VSGraphModel)graphModel;
            IStickyNoteModel[] stickyNotesToDelete = GetStickyNotesToDelete(deletables);
            IPlacematModel[] placematsToDelete = GetPlacematsToDelete(deletables);
            IReadOnlyCollection<INodeModel> nodesToDelete = GetNodesToDelete(vsGraphModel, deletables);
            IReadOnlyCollection<IEdgeModel> edgesToDelete = GetEdgesToDelete(graphModel, deletables, nodesToDelete);
            VariableDeclarationModel[] declarationModelsToDelete = GetDeclarationModelsToDelete(deletables, nodesToDelete);

            if (declarationModelsToDelete.Any())
                previousState.MarkForUpdate(UpdateFlags.RequestRebuild);

            graphModel.DeleteStickyNotes(stickyNotesToDelete);
            graphModel.DeletePlacemats(placematsToDelete);
            graphModel.DeleteEdges(edgesToDelete);
            graphModel.DeleteNodes(nodesToDelete, GraphModel.DeleteConnections.False);
            vsGraphModel.DeleteVariableDeclarations(declarationModelsToDelete, false);
        }

        static VariableDeclarationModel[] GetDeclarationModelsToDelete(IGTFGraphElementModel[] deletables, IReadOnlyCollection<INodeModel> nodesToDelete)
        {
            return deletables.OfType<VariableDeclarationModel>().ToArray();
        }

        static IStickyNoteModel[] GetStickyNotesToDelete(IGTFGraphElementModel[] deletables)
        {
            return deletables.OfType<IStickyNoteModel>().ToArray();
        }

        static IPlacematModel[] GetPlacematsToDelete(IGTFGraphElementModel[] deletables)
        {
            return deletables.OfType<IPlacematModel>().ToArray();
        }

        static List<IEdgeModel> GetEdgesToDelete(GraphModel graphModel, IGTFGraphElementModel[] deletables, IReadOnlyCollection<INodeModel> nodesToDelete)
        {
            var edgesToDelete = new HashSet<IEdgeModel>(deletables.OfType<IEdgeModel>());
            foreach (var node in nodesToDelete)
                foreach (var portModel in node.GetPortModels())
                    edgesToDelete.AddRange(graphModel.EdgesConnectedToPorts(portModel));
            return edgesToDelete.ToList();
        }

        static List<INodeModel> GetNodesToDelete(VSGraphModel vsGraphModel, IGTFGraphElementModel[] deletables)
        {
            var nodesToDelete = new HashSet<INodeModel>(deletables.OfType<NodeModel>());
            nodesToDelete.AddRange(deletables.OfType<VariableDeclarationModel>().SelectMany(vsGraphModel.FindUsages<VariableNodeModel>));
            return nodesToDelete.ToList();
        }

        static State BypassAndDeleteElements(State previousState, RemoveNodesAction action)
        {
            var graphModel = (VSGraphModel)previousState.CurrentGraphModel;

            Undo.RegisterCompleteObjectUndo((Object)graphModel.AssetModel, "Delete elements");

            graphModel.BypassNodes(action.NodesToBypass);
            List<IGTFGraphElementModel> graphElementsToRemove = action.ElementsToRemove.Cast<IGTFGraphElementModel>().ToList();
            DeleteElementsFromGraph(previousState, graphElementsToRemove, graphModel);
            return previousState;
        }

        static State RenameElement(State previousState, RenameElementAction action)
        {
            var graphModel = (VSGraphModel)previousState.CurrentGraphModel;

            if (string.IsNullOrWhiteSpace(action.Name) && action.RenamableModel is INodeModel)
                return previousState;

            Undo.RegisterCompleteObjectUndo((Object)graphModel.AssetModel, "Rename");
            action.RenamableModel.Rename(action.Name);
            EditorUtility.SetDirty((Object)graphModel.AssetModel);

            IGraphChangeList graphChangeList = previousState.CurrentGraphModel.LastChanges;

            VSGraphModel vsGraphModel = (VSGraphModel)previousState.CurrentGraphModel;

            if (action.RenamableModel is VariableDeclarationModel variableDeclarationModel)
            {
                graphChangeList.BlackBoardChanged = true;

                // update usage names
                graphChangeList.ChangedElements.AddRange(vsGraphModel.FindUsages<VariableNodeModel>(variableDeclarationModel));
            }
            else if (action.RenamableModel is IVariableModel variableModel)
            {
                graphChangeList.BlackBoardChanged = true;

                variableDeclarationModel = variableModel.DeclarationModel as VariableDeclarationModel;
                graphChangeList.ChangedElements.Add(variableDeclarationModel);

                graphChangeList.ChangedElements.AddRange(vsGraphModel.FindUsages<VariableNodeModel>(variableDeclarationModel));
            }
            else if (action.RenamableModel is IEdgePortalModel edgePortalModel)
            {
                variableDeclarationModel = edgePortalModel.DeclarationModel as VariableDeclarationModel;
                graphChangeList.ChangedElements.Add(variableDeclarationModel);
                graphChangeList.ChangedElements.AddRange(vsGraphModel.FindUsages<EdgePortalModel>(variableDeclarationModel));
            }
            else
                graphChangeList.ChangedElements.Add(action.RenamableModel as IGraphElementModel);


            previousState.MarkForUpdate(UpdateFlags.RequestCompilation | UpdateFlags.RequestRebuild);

            return previousState;
        }

        static State MoveElements(State previousState, MoveElementsAction action)
        {
            if (action.Models == null || action.Delta == Vector2.zero)
                return previousState;

            Undo.RegisterCompleteObjectUndo((Object)previousState.AssetModel, "Move");

            foreach (var placematModel in action.Models.OfType<IGTFPlacematModel>())
                placematModel.Move(action.Delta);

            // TODO It would be nice to have a single way of moving things around and thus not having to deal with 3
            // separate collections.
            foreach (var nodeModel in action.Models.OfType<INodeModel>())
                ((GraphModel)previousState.CurrentGraphModel).MoveNode(nodeModel, nodeModel.Position + action.Delta);

            foreach (var stickyNoteModel in action.Models.OfType<IGTFStickyNoteModel>())
                stickyNoteModel.PositionAndSize = new Rect(stickyNoteModel.PositionAndSize.position + action.Delta, stickyNoteModel.PositionAndSize.size);

            // Only move an edge if it is connected on both ends to a moving node.
            var edgeModels = action.Models.OfType<IGTFEdgeModel>();
            if (edgeModels.Any())
            {
                var nodeModels = action.Models.OfType<IGTFNodeModel>().ToImmutableHashSet();
                foreach (var edgeModel in edgeModels)
                {
                    if (nodeModels.Contains(edgeModel.FromPort.NodeModel) && nodeModels.Contains(edgeModel.ToPort.NodeModel))
                    {
                        edgeModel.Move(action.Delta);
                    }
                }
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

        static State ChangeElementColor(State previousState, ChangeElementColorAction action)
        {
            Undo.RegisterCompleteObjectUndo((Object)previousState.AssetModel, "Change Color");
            EditorUtility.SetDirty((Object)previousState.AssetModel);
            if (action.NodeModels != null)
                foreach (var model in action.NodeModels)
                {
                    model.ChangeColor(action.Color);
                    previousState.MarkForUpdate(UpdateFlags.UpdateView, model);
                }
            if (action.PlacematModels != null)
                foreach (var model in action.PlacematModels)
                {
                    model.Color = action.Color;
                    previousState.MarkForUpdate(UpdateFlags.UpdateView, model);
                }
            return previousState;
        }

        static State ResetElementColor(State previousState, ResetElementColorAction action)
        {
            Undo.RegisterCompleteObjectUndo((Object)previousState.AssetModel, "Reset Color");
            EditorUtility.SetDirty((Object)previousState.AssetModel);
            if (action.NodeModels != null)
                foreach (var model in action.NodeModels)
                {
                    model.HasUserColor = false;
                    previousState.MarkForUpdate(UpdateFlags.UpdateView, model);
                }
            if (action.PlacematModels != null)
                foreach (var model in action.PlacematModels)
                {
                    model.Color = PlacematModel.k_DefaultColor;
                    previousState.MarkForUpdate(UpdateFlags.UpdateView, model);
                }
            return previousState;
        }
    }
}
