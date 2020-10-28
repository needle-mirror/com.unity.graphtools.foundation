using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public class MoveElementsAction : ModelAction<IMovable, Vector2>
    {
        const string k_UndoStringSingular = "Move Element";
        const string k_UndoStringPlural = "Move Elements";

        public MoveElementsAction()
            : base(k_UndoStringSingular) {}

        public MoveElementsAction(Vector2 delta, IReadOnlyList<IMovable> models)
            : base(k_UndoStringSingular, k_UndoStringPlural, models, delta) {}

        public static void DefaultReducer(State previousState, MoveElementsAction action)
        {
            if (action.Models == null || action.Value == Vector2.zero)
                return;

            previousState.PushUndo(action);

            foreach (var placematModel in action.Models.OfType<IPlacematModel>())
                placematModel.Move(action.Value);

            // TODO It would be nice to have a single way of moving things around and thus not having to deal with 3
            // separate collections.
            foreach (var nodeModel in action.Models.OfType<INodeModel>())
            {
                nodeModel.Move(nodeModel.Position + action.Value);
            }

            foreach (var stickyNoteModel in action.Models.OfType<IStickyNoteModel>())
                stickyNoteModel.PositionAndSize = new Rect(stickyNoteModel.PositionAndSize.position + action.Value, stickyNoteModel.PositionAndSize.size);

            // Only move an edge if it is connected on both ends to a moving node.
            var edgeModels = action.Models.OfType<IEdgeModel>();
            if (edgeModels.Any())
            {
                var nodeModels = new HashSet<INodeModel>(action.Models.OfType<INodeModel>());
                foreach (var edgeModel in edgeModels)
                {
                    if (edgeModel is IEditableEdge editableEdge && nodeModels.Contains(edgeModel.FromPort.NodeModel) && nodeModels.Contains(edgeModel.ToPort.NodeModel))
                    {
                        editableEdge.Move(action.Value);
                    }
                }
            }


            previousState.MarkForUpdate(UpdateFlags.GraphGeometry);
        }
    }

    public class AutoPlaceElementsAction : ModelAction<IMovable>
    {
        const string k_UndoStringSingular = "Auto Place Element";
        const string k_UndoStringPlural = "Auto Place Elements";

        public IReadOnlyList<Vector2> Deltas;

        public AutoPlaceElementsAction()
            : base(k_UndoStringSingular) {}

        public AutoPlaceElementsAction(IReadOnlyList<Vector2> delta, IReadOnlyList<IMovable> models)
            : base(k_UndoStringSingular, k_UndoStringPlural, models)
        {
            Deltas = delta;
        }

        public static void DefaultReducer(State previousState, AutoPlaceElementsAction action)
        {
            if (action.Models == null || action.Deltas == null || action.Models.Count != action.Deltas.Count)
                return;

            previousState.PushUndo(action);

            for (int i = 0; i < action.Models.Count; ++i)
            {
                IMovable model = action.Models[i];
                Vector2 delta = action.Deltas[i];
                switch (model)
                {
                    case IPlacematModel _:
                        model.Move(delta);
                        break;
                    case INodeModel nodeModel:
                        nodeModel.Move(nodeModel.Position + delta);
                        break;
                    case IStickyNoteModel noteModel:
                        noteModel.PositionAndSize = new Rect(noteModel.PositionAndSize.position + delta, noteModel.PositionAndSize.size);
                        break;
                }
                previousState.MarkForUpdate(UpdateFlags.UpdateView, model as IGraphElementModel);
            }
        }
    }

    public class DeleteElementsAction : ModelAction<IGraphElementModel>
    {
        const string k_UndoStringSingular = "Delete Element";
        const string k_UndoStringPlural = "Delete Elements";

        public DeleteElementsAction()
            : base(k_UndoStringSingular) {}

        public DeleteElementsAction(IReadOnlyList<IGraphElementModel> elementsToRemove)
            : base(k_UndoStringSingular, k_UndoStringPlural, elementsToRemove)
        {
        }

        public static void DefaultReducer(State previousState, DeleteElementsAction action)
        {
            if (!action.Models.Any())
                return;

            previousState.PushUndo(action);
            DeleteElementsFromGraph(previousState, action.Models, previousState.CurrentGraphModel);
        }

        internal static void DeleteElementsFromGraph(State previousState, IReadOnlyCollection<IGraphElementModel> elementsToRemove, IGraphModel graphModel)
        {
            IGraphElementModel[] deletables = elementsToRemove.Where(x => x.IsDeletable()).Distinct().ToArray();

            IStickyNoteModel[] stickyNotesToDelete = GetStickyNotesToDelete(deletables);
            IPlacematModel[] placematsToDelete = GetPlacematsToDelete(deletables);
            IReadOnlyCollection<INodeModel> nodesToDelete = GetNodesToDelete(graphModel, deletables);
            IReadOnlyCollection<IEdgeModel> edgesToDelete = GetEdgesToDelete(graphModel, deletables, nodesToDelete);
            IVariableDeclarationModel[] declarationModelsToDelete = GetDeclarationModelsToDelete(deletables);

            if (declarationModelsToDelete.Any())
                previousState.MarkForUpdate(UpdateFlags.RequestRebuild);

            graphModel.DeleteStickyNotes(stickyNotesToDelete);
            graphModel.DeletePlacemats(placematsToDelete);
            graphModel.DeleteEdges(edgesToDelete);
            graphModel.DeleteNodes(nodesToDelete, DeleteConnections.False);
            graphModel.DeleteVariableDeclarations(declarationModelsToDelete, false);
        }

        static IVariableDeclarationModel[] GetDeclarationModelsToDelete(IGraphElementModel[] deletables)
        {
            return deletables.OfType<IVariableDeclarationModel>().ToArray();
        }

        static IStickyNoteModel[] GetStickyNotesToDelete(IGraphElementModel[] deletables)
        {
            return deletables.OfType<IStickyNoteModel>().ToArray();
        }

        static IPlacematModel[] GetPlacematsToDelete(IGraphElementModel[] deletables)
        {
            return deletables.OfType<IPlacematModel>().ToArray();
        }

        static List<IEdgeModel> GetEdgesToDelete(IGraphModel graphModel, IGraphElementModel[] deletables, IReadOnlyCollection<INodeModel> nodesToDelete)
        {
            var edgesToDelete = new HashSet<IEdgeModel>(deletables.OfType<IEdgeModel>());
            foreach (var node in nodesToDelete)
            {
                var portHolder = node as IPortNode;
                foreach (var portModel in portHolder?.Ports ?? Enumerable.Empty<IPortModel>())
                {
                    var edgesConnectedToPort = graphModel.EdgeModels.Where(e => e.ToPort == portModel || e.FromPort == portModel);
                    edgesToDelete.AddRange(edgesConnectedToPort);
                }
            }

            return edgesToDelete.ToList();
        }

        static List<INodeModel> GetNodesToDelete(IGraphModel vsGraphModel, IGraphElementModel[] deletables)
        {
            var nodesToDelete = new HashSet<INodeModel>(deletables.OfType<INodeModel>());
            nodesToDelete.AddRange(deletables.OfType<IVariableDeclarationModel>().SelectMany(vsGraphModel.FindReferencesInGraph<IVariableNodeModel>));
            return nodesToDelete.ToList();
        }
    }

    public class BuildAllEditorAction : BaseAction
    {
        public BuildAllEditorAction()
        {
            UndoString = "Compile Graph";
        }

        public static void DefaultReducer(State previousState, BuildAllEditorAction action)
        {
        }
    }

    public struct TargetInsertionInfo
    {
        public Vector2 Delta;
        public string OperationName;
    }

    public class PasteSerializedDataAction : BaseAction
    {
        public readonly IGraphModel Graph;
        public readonly TargetInsertionInfo Info;
        public readonly IEditorDataModel EditorDataModel;
        public readonly GtfoGraphView.CopyPasteData Data;

        public PasteSerializedDataAction()
        {
            UndoString = "Paste";
        }

        public PasteSerializedDataAction(IGraphModel graph, TargetInsertionInfo info, IEditorDataModel editorDataModel,
                                         GtfoGraphView.CopyPasteData data) : this()
        {
            Graph = graph;
            Info = info;
            EditorDataModel = editorDataModel;
            Data = data;
        }

        public static void DefaultReducer(State previousState, PasteSerializedDataAction action)
        {
            previousState.PushUndo(action);
            GtfoGraphView.PasteSerializedData(action.Graph, action.Info, action.EditorDataModel, action.Data);
        }
    }
}
