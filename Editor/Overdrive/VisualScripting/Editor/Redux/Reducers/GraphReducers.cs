using System;
using System.Collections.Generic;
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
            store.RegisterReducer<State, RenameElementAction>(RenameElement);
            store.RegisterReducer<State, DeleteElementsAction>(DeleteElements);
            store.RegisterReducer<State, UpdatePortConstantAction>(UpdatePortConstant);
            store.RegisterReducer<State, RemoveNodesAction>(BypassAndDeleteElements);
            store.RegisterReducer<State, PasteSerializedDataAction>(PasteSerializedData);
            store.RegisterReducer<State, MoveElementsAction>(MoveElements);
            store.RegisterReducer<State, AlignElementsAction>(AlignElements);
            store.RegisterReducer<State, PanToNodeAction>(PanToNode);
            store.RegisterReducer<State, ChangeElementColorAction>(ChangeElementColor);
            store.RegisterReducer<State, ResetElementColorAction>(ResetElementColor);
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

        static void DeleteElementsFromGraph(State previousState, IReadOnlyCollection<IGTFGraphElementModel> elementsToRemove, IGTFGraphModel graphModel)
        {
            IGTFGraphElementModel[] deletables = elementsToRemove.Where(x => x is IDeletable).Distinct().ToArray();

            IGTFStickyNoteModel[] stickyNotesToDelete = GetStickyNotesToDelete(deletables);
            IGTFPlacematModel[] placematsToDelete = GetPlacematsToDelete(deletables);
            IReadOnlyCollection<IGTFNodeModel> nodesToDelete = GetNodesToDelete(graphModel, deletables);
            IReadOnlyCollection<IGTFEdgeModel> edgesToDelete = GetEdgesToDelete(graphModel, deletables, nodesToDelete);
            VariableDeclarationModel[] declarationModelsToDelete = GetDeclarationModelsToDelete(deletables);

            if (declarationModelsToDelete.Any())
                previousState.MarkForUpdate(UpdateFlags.RequestRebuild);

            graphModel.DeleteStickyNotes(stickyNotesToDelete);
            graphModel.DeletePlacemats(placematsToDelete);
            graphModel.DeleteEdges(edgesToDelete);
            graphModel.DeleteNodes(nodesToDelete, DeleteConnections.False);
            graphModel.DeleteVariableDeclarations(declarationModelsToDelete, false, true);
        }

        static VariableDeclarationModel[] GetDeclarationModelsToDelete(IGTFGraphElementModel[] deletables)
        {
            return deletables.OfType<VariableDeclarationModel>().ToArray();
        }

        static IGTFStickyNoteModel[] GetStickyNotesToDelete(IGTFGraphElementModel[] deletables)
        {
            return deletables.OfType<IGTFStickyNoteModel>().ToArray();
        }

        static IGTFPlacematModel[] GetPlacematsToDelete(IGTFGraphElementModel[] deletables)
        {
            return deletables.OfType<IGTFPlacematModel>().ToArray();
        }

        static List<IGTFEdgeModel> GetEdgesToDelete(IGTFGraphModel graphModel, IGTFGraphElementModel[] deletables, IReadOnlyCollection<IGTFNodeModel> nodesToDelete)
        {
            var edgesToDelete = new HashSet<IGTFEdgeModel>(deletables.OfType<IGTFEdgeModel>());
            foreach (var node in nodesToDelete)
            {
                foreach (var portModel in node.GetPortModels())
                {
                    var edgesConnectedToPort = graphModel.EdgeModels.Where(e => e.ToPort == portModel || e.FromPort == portModel);
                    edgesToDelete.AddRange(edgesConnectedToPort);
                }
            }
            return edgesToDelete.ToList();
        }

        static List<IGTFNodeModel> GetNodesToDelete(IGTFGraphModel vsGraphModel, IGTFGraphElementModel[] deletables)
        {
            var nodesToDelete = new HashSet<IGTFNodeModel>(deletables.OfType<NodeModel>());
            nodesToDelete.AddRange(deletables.OfType<VariableDeclarationModel>().SelectMany(vsGraphModel.FindReferencesInGraph<VariableNodeModel>));
            return nodesToDelete.ToList();
        }

        static void BypassNodes(IGTFGraphModel graphModel, IGTFNodeModel[] actionNodeModels)
        {
            foreach (var model in actionNodeModels)
            {
                var inputEdgeModels = new List<IGTFEdgeModel>();
                foreach (var portModel in model.InputsByDisplayOrder)
                {
                    inputEdgeModels.AddRange(graphModel.GetEdgesConnections(portModel));
                }

                if (!inputEdgeModels.Any())
                    continue;

                var outputEdgeModels = new List<IGTFEdgeModel>();
                foreach (var portModel in model.OutputsByDisplayOrder)
                {
                    outputEdgeModels.AddRange(graphModel.GetEdgesConnections(portModel));
                }

                if (!outputEdgeModels.Any())
                    continue;

                graphModel.DeleteEdges(inputEdgeModels);
                graphModel.DeleteEdges(outputEdgeModels);

                graphModel.CreateEdge(outputEdgeModels[0].ToPort, inputEdgeModels[0].FromPort);
            }
        }

        static State BypassAndDeleteElements(State previousState, RemoveNodesAction action)
        {
            var graphModel = previousState.CurrentGraphModel;

            Undo.RegisterCompleteObjectUndo((Object)graphModel.AssetModel, "Delete elements");

            BypassNodes(graphModel, action.NodesToBypass);
            List<IGTFGraphElementModel> graphElementsToRemove = action.ElementsToRemove.Cast<IGTFGraphElementModel>().ToList();
            DeleteElementsFromGraph(previousState, graphElementsToRemove, graphModel);
            return previousState;
        }

        static State UpdatePortConstant<TState>(TState previousState, UpdatePortConstantAction action) where TState : State
        {
            previousState.MarkForUpdate(UpdateFlags.RequestCompilation);

            var graphAsset = action.PortModel.AssetModel;
            EditorUtility.SetDirty(graphAsset as Object);

            Undo.RegisterCompleteObjectUndo((Object)previousState.AssetModel, "Update port constant");
            if (action.PortModel.EmbeddedValue is IGTFStringWrapperConstantModel stringWrapperConstantModel)
                stringWrapperConstantModel.StringValue = (string)action.NewValue;
            else
                action.PortModel.EmbeddedValue.ObjectValue = action.NewValue;

            return previousState;
        }

        static State RenameElement(State previousState, RenameElementAction action)
        {
            var graphModel = previousState.CurrentGraphModel;

            if (string.IsNullOrWhiteSpace(action.Name) && action.RenamableModel is IGTFNodeModel)
                return previousState;

            Undo.RegisterCompleteObjectUndo((Object)graphModel.AssetModel, "Rename");
            action.RenamableModel.Rename(action.Name);
            EditorUtility.SetDirty((Object)graphModel.AssetModel);

            IGraphChangeList graphChangeList = previousState.CurrentGraphModel.LastChanges;

            var vsGraphModel = previousState.CurrentGraphModel;

            if (action.RenamableModel is VariableDeclarationModel variableDeclarationModel)
            {
                graphChangeList.BlackBoardChanged = true;

                // update usage names
                graphChangeList.ChangedElements.AddRange(vsGraphModel.FindReferencesInGraph<VariableNodeModel>(variableDeclarationModel));
            }
            else if (action.RenamableModel is IGTFVariableNodeModel variableModel)
            {
                graphChangeList.BlackBoardChanged = true;

                variableDeclarationModel = variableModel.VariableDeclarationModel as VariableDeclarationModel;
                graphChangeList.ChangedElements.Add(variableDeclarationModel);

                graphChangeList.ChangedElements.AddRange(vsGraphModel.FindReferencesInGraph<VariableNodeModel>(variableDeclarationModel));
            }
            else if (action.RenamableModel is IGTFEdgePortalModel edgePortalModel)
            {
                var declarationModel = edgePortalModel.DeclarationModel as IGTFGraphElementModel;
                graphChangeList.ChangedElements.Add(declarationModel);
                graphChangeList.ChangedElements.AddRange(vsGraphModel.FindReferencesInGraph<EdgePortalModel>(edgePortalModel.DeclarationModel));
            }
            else
                graphChangeList.ChangedElements.Add(action.RenamableModel as IGTFGraphElementModel);


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
            foreach (var nodeModel in action.Models.OfType<IGTFNodeModel>())
                ((GraphModel)previousState.CurrentGraphModel).MoveNode(nodeModel, nodeModel.Position + action.Delta);

            foreach (var stickyNoteModel in action.Models.OfType<IGTFStickyNoteModel>())
                stickyNoteModel.PositionAndSize = new Rect(stickyNoteModel.PositionAndSize.position + action.Delta, stickyNoteModel.PositionAndSize.size);

            // Only move an edge if it is connected on both ends to a moving node.
            var edgeModels = action.Models.OfType<IGTFEdgeModel>();
            if (edgeModels.Any())
            {
                var nodeModels = new HashSet<IGTFNodeModel>(action.Models.OfType<IGTFNodeModel>());
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

        static State AlignElements(State previousState, AlignElementsAction action)
        {
            if (action.Models == null || action.Deltas == null || action.Models.Length != action.Deltas.Length)
                return previousState;

            Undo.RegisterCompleteObjectUndo((Object)previousState.AssetModel, "Align");

            for (int i = 0; i < action.Models.Length; ++i)
            {
                IPositioned model = action.Models[i];
                Vector2 delta = action.Deltas[i];
                switch (model)
                {
                    case IGTFPlacematModel _:
                        model.Move(delta);
                        break;
                    case IGTFNodeModel nodeModel:
                        ((GraphModel)previousState.CurrentGraphModel).MoveNode(nodeModel, nodeModel.Position + delta);
                        break;
                    case IGTFStickyNoteModel noteModel:
                        noteModel.PositionAndSize = new Rect(noteModel.PositionAndSize.position + delta, noteModel.PositionAndSize.size);
                        break;
                }
                previousState.MarkForUpdate(UpdateFlags.UpdateView, model as IGTFGraphElementModel);
            }

            if (previousState.AssetModel != null)
            {
                EditorUtility.SetDirty((Object)previousState.AssetModel);
            }

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
