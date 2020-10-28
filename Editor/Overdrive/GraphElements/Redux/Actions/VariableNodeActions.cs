using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public class CreateVariableNodesAction : BaseAction
    {
        public List<(IVariableDeclarationModel, SerializableGUID, Vector2)> VariablesToCreate;
        public IPortModel ConnectAfterCreation;
        public IReadOnlyList<IEdgeModel> EdgeModelsToDelete;
        public bool AutoAlign;

        public CreateVariableNodesAction()
        {
            UndoString = "Create Variable Node";
        }

        public CreateVariableNodesAction(List<(IVariableDeclarationModel, SerializableGUID, Vector2)> variablesToCreate,
                                         bool autoAlign = false) : this()
        {
            VariablesToCreate = variablesToCreate;
            AutoAlign = autoAlign;
        }

        public CreateVariableNodesAction(IVariableDeclarationModel graphElementModel, Vector2 mousePosition,
                                         IReadOnlyList<IEdgeModel> edgeModelsToDelete = null, IPortModel connectAfterCreation = null,
                                         bool autoAlign = false) : this()
        {
            VariablesToCreate = new List<(IVariableDeclarationModel, SerializableGUID, Vector2)>();
            VariablesToCreate.Add((graphElementModel, GUID.Generate(), mousePosition));
            EdgeModelsToDelete = edgeModelsToDelete;
            ConnectAfterCreation = connectAfterCreation;
            AutoAlign = autoAlign;
        }

        public static void DefaultReducer(State previousState, CreateVariableNodesAction action)
        {
            if (action.VariablesToCreate.Count > 0)
            {
                previousState.PushUndo(action);

                if (action.ConnectAfterCreation != null)
                {
                    // Delete previous connections
                    if (action.EdgeModelsToDelete.Any())
                    {
                        previousState.CurrentGraphModel.DeleteEdges(action.EdgeModelsToDelete);
                    }
                }

                foreach ((IVariableDeclarationModel, SerializableGUID, Vector2)tuple in action.VariablesToCreate)
                {
                    var vsGraphModel = previousState.CurrentGraphModel;

                    IVariableNodeModel newVariable = vsGraphModel.CreateVariableNode(tuple.Item1, tuple.Item3, guid: tuple.Item2);

                    if (action.ConnectAfterCreation != null)
                    {
                        var newEdge = previousState.CurrentGraphModel.CreateEdge(action.ConnectAfterCreation, newVariable.OutputPort);
                        if (action.AutoAlign)
                        {
                            vsGraphModel.LastChanges.ElementsToAutoAlign.Add(newEdge);
                        }
                    }
                }
            }
        }
    }

    public class ConvertVariableNodesToConstantNodesAction : ModelAction<IVariableNodeModel>
    {
        const string k_UndoStringSingular = "Convert Variable To Constant";
        const string k_UndoStringPlural = "Convert Variables To Constants";

        public ConvertVariableNodesToConstantNodesAction()
            : base(k_UndoStringSingular) {}

        public ConvertVariableNodesToConstantNodesAction(params IVariableNodeModel[] variableModels)
            : base(k_UndoStringSingular, k_UndoStringPlural, variableModels) {}

        public static void DefaultReducer(State previousState, ConvertVariableNodesToConstantNodesAction action)
        {
            if (!action.Models.Any())
                return;

            previousState.PushUndo(action);

            var graphModel = previousState.CurrentGraphModel;
            int variableModelsCount = action.Models.Count;
            previousState.EditorDataModel.ElementModelToRename = null;
            foreach (var variableModel in action.Models)
            {
                if (graphModel.Stencil.GetConstantNodeValueType(variableModel.GetDataType()) == null)
                    continue;
                var constantNode = graphModel.CreateConstantNode(variableModel.Title, variableModel.GetDataType(), variableModel.Position);
                // Rename converted item only if there are no other items to be converted
                if (variableModelsCount == 1)
                    previousState.EditorDataModel.ElementModelToRename = constantNode;
                constantNode.ObjectValue = variableModel.VariableDeclarationModel?.InitializationModel?.ObjectValue;

                foreach (var edge in graphModel.GetEdgesConnections(variableModel.OutputPort).ToList())
                {
                    graphModel.CreateEdge(edge.ToPort, constantNode.OutputPort);
                    graphModel.DeleteEdge(edge);
                }

                graphModel.DeleteNode(variableModel, DeleteConnections.False);
            }
        }
    }

    public class ConvertConstantNodesToVariableNodesAction : ModelAction<IConstantNodeModel>
    {
        const string k_UndoStringSingular = "Convert Constant To Variable";
        const string k_UndoStringPlural = "Convert Constants To Variables";

        public ConvertConstantNodesToVariableNodesAction()
            : base(k_UndoStringSingular) {}

        public ConvertConstantNodesToVariableNodesAction(params IConstantNodeModel[] constantModels)
            : base(k_UndoStringSingular, k_UndoStringPlural, constantModels) {}

        public static void DefaultReducer(State previousState, ConvertConstantNodesToVariableNodesAction action)
        {
            if (!action.Models.Any())
                return;

            previousState.PushUndo(action);

            var graphModel = previousState.CurrentGraphModel;
            foreach (var constantModel in action.Models)
            {
                var declarationModel = graphModel.CreateGraphVariableDeclaration(StringExtensions.CodifyString(constantModel.Type.FriendlyName()), constantModel.Type.GenerateTypeHandle(), ModifierFlags.None, true, constantModel.Value.CloneConstant());
                if (graphModel.CreateVariableNode(declarationModel, constantModel.Position) is IVariableNodeModel variableModel)
                {
                    foreach (var edge in graphModel.GetEdgesConnections(constantModel.OutputPort).ToList())
                    {
                        graphModel.CreateEdge(edge.ToPort, variableModel.OutputPort);
                        graphModel.DeleteEdge(edge);
                    }
                }

                graphModel.DeleteNode(constantModel, DeleteConnections.False);
            }
        }
    }

    // Create a separate instance of the constant or variable node for each connections on the original variable node.
    public class ItemizeNodeAction : ModelAction<ISingleOutputPortNode>
    {
        const string k_UndoStringSingular = "Itemize Node";
        const string k_UndoStringPlural = "Itemize Nodes";

        public ItemizeNodeAction()
            : base(k_UndoStringSingular) {}

        public ItemizeNodeAction(params ISingleOutputPortNode[] models)
            : base(k_UndoStringSingular, k_UndoStringPlural, models) {}

        public static void DefaultReducer(State previousState, ItemizeNodeAction action)
        {
            bool undoPushed = false;

            var graphModel = previousState.CurrentGraphModel;
            foreach (var model in action.Models)
            {
                var edges = graphModel.GetEdgesConnections(model.OutputPort).ToList();

                if (!(model is IVariableNodeModel) && !(model is IConstantNodeModel))
                    continue;

                for (var i = 1; i < edges.Count; i++)
                {
                    if (!undoPushed)
                    {
                        undoPushed = true;
                        previousState.PushUndo(action);
                    }

                    ISingleOutputPortNode newModel = null;
                    if (model is IVariableNodeModel variableNodeModel)
                    {
                        newModel = graphModel.CreateVariableNode(variableNodeModel.VariableDeclarationModel,
                            model.Position + i * 50 * Vector2.up);
                    }
                    else if (model is IConstantNodeModel constantNodeModel)
                    {
                        var currentName = (constantNodeModel as IHasTitle)?.Title;
                        var newName = (string.IsNullOrEmpty(currentName) ? "Temporary" : currentName) + i;

                        newModel = graphModel.CreateConstantNode(newName, constantNodeModel.Type.GenerateTypeHandle(),
                            constantNodeModel.Position + i * 50 * Vector2.up);
                    }

                    var edge = edges[i];
                    graphModel.CreateEdge(edge.ToPort, newModel.OutputPort);
                    graphModel.DeleteEdge(edge);
                }
            }
        }
    }

    public class ToggleLockConstantNodeAction : ModelAction<IConstantNodeModel>
    {
        const string k_UndoStringSingular = "Toggle Lock Constant";
        const string k_UndoStringPlural = "Toggle Lock Constants";

        public ToggleLockConstantNodeAction()
            : base(k_UndoStringSingular) {}

        public ToggleLockConstantNodeAction(params IConstantNodeModel[] constantNodeModels)
            : base(k_UndoStringSingular, k_UndoStringPlural, constantNodeModels) {}

        public static void DefaultReducer(State previousState, ToggleLockConstantNodeAction action)
        {
            if (!action.Models.Any())
                return;

            previousState.PushUndo(action);

            foreach (var constantNodeModel in action.Models)
            {
                constantNodeModel.IsLocked = !constantNodeModel.IsLocked;
            }

            previousState.CurrentGraphModel.LastChanges.ChangedElements.AddRange(action.Models);
        }
    }
}
