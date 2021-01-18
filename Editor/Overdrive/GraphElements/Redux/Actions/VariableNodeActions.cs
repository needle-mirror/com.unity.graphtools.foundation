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

        public CreateVariableNodesAction(List<(IVariableDeclarationModel, SerializableGUID, Vector2)> variablesToCreate) : this()
        {
            VariablesToCreate = variablesToCreate;
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

        public static void DefaultReducer(State state, CreateVariableNodesAction action)
        {
            if (action.VariablesToCreate.Count > 0)
            {
                state.PushUndo(action);

                if (action.ConnectAfterCreation != null)
                {
                    // Delete previous connections
                    if (action.EdgeModelsToDelete.Any())
                    {
                        state.GraphModel.DeleteEdges(action.EdgeModelsToDelete);
                        state.MarkDeleted(action.EdgeModelsToDelete);
                    }
                }

                foreach (var(variableDeclarationModel, guid, position) in action.VariablesToCreate)
                {
                    var vsGraphModel = state.GraphModel;

                    var newVariable = vsGraphModel.CreateVariableNode(variableDeclarationModel, position, guid: guid);
                    state.MarkNew(newVariable);

                    if (action.ConnectAfterCreation != null)
                    {
                        var newEdge = state.GraphModel.CreateEdge(action.ConnectAfterCreation, newVariable.OutputPort);
                        state.MarkNew(newEdge);
                        if (action.AutoAlign)
                        {
                            state.MarkModelToAutoAlign(newEdge);
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

        public static void DefaultReducer(State state, ConvertVariableNodesToConstantNodesAction action)
        {
            if (!action.Models.Any())
                return;

            state.PushUndo(action);

            var graphModel = state.GraphModel;
            foreach (var variableModel in action.Models)
            {
                if (graphModel.Stencil.GetConstantNodeValueType(variableModel.GetDataType()) == null)
                    continue;
                var constantModel = graphModel.CreateConstantNode(variableModel.GetDataType(), variableModel.Title, variableModel.Position);
                constantModel.ObjectValue = variableModel.VariableDeclarationModel?.InitializationModel?.ObjectValue;
                constantModel.State = variableModel.State;
                constantModel.HasUserColor = variableModel.HasUserColor;
                if (variableModel.HasUserColor)
                    constantModel.Color = variableModel.Color;
                state.MarkNew(constantModel);

                var edgeModels = graphModel.GetEdgesConnections(variableModel.OutputPort).ToList();
                foreach (var edge in edgeModels)
                {
                    var newEdge = graphModel.CreateEdge(edge.ToPort, constantModel.OutputPort);
                    var deletedModels = graphModel.DeleteEdge(edge);
                    state.MarkNew(newEdge);
                    state.MarkDeleted(deletedModels);
                }

                var deletedElements = graphModel.DeleteNode(variableModel, deleteConnections: false);
                state.MarkDeleted(deletedElements);
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

        public static void DefaultReducer(State state, ConvertConstantNodesToVariableNodesAction action)
        {
            if (!action.Models.Any())
                return;

            state.PushUndo(action);

            var graphModel = state.GraphModel;
            foreach (var constantModel in action.Models)
            {
                var declarationModel = graphModel.CreateGraphVariableDeclaration(
                    constantModel.Type.GenerateTypeHandle(),
                    StringExtensions.CodifyString(constantModel.Type.FriendlyName()), ModifierFlags.None, true,
                    constantModel.Value.CloneConstant());
                state.MarkNew(declarationModel);

                if (graphModel.CreateVariableNode(declarationModel, constantModel.Position) is IVariableNodeModel variableModel)
                {
                    state.MarkNew(variableModel);

                    variableModel.State = constantModel.State;
                    variableModel.HasUserColor = constantModel.HasUserColor;
                    if (constantModel.HasUserColor)
                        variableModel.Color = constantModel.Color;
                    foreach (var edge in graphModel.GetEdgesConnections(constantModel.OutputPort).ToList())
                    {
                        var newEdge = graphModel.CreateEdge(edge.ToPort, variableModel.OutputPort);
                        var deleteModels = graphModel.DeleteEdge(edge);

                        state.MarkNew(newEdge);
                        state.MarkDeleted(deleteModels);
                    }
                }

                var deletedElements  = graphModel.DeleteNode(constantModel, deleteConnections: false);
                state.MarkDeleted(deletedElements);
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

        public static void DefaultReducer(State state, ItemizeNodeAction action)
        {
            bool undoPushed = false;

            var graphModel = state.GraphModel;
            foreach (var model in action.Models.Where(m => m is IVariableNodeModel || m is IConstantNodeModel))
            {
                var edges = graphModel.GetEdgesConnections(model.OutputPort).ToList();

                for (var i = 1; i < edges.Count; i++)
                {
                    if (!undoPushed)
                    {
                        undoPushed = true;
                        state.PushUndo(action);
                    }

                    var newModel = (ISingleOutputPortNode)graphModel.DuplicateNode(model, i * 50 * Vector2.up);
                    state.MarkNew(newModel);
                    var edge = edges[i];
                    var newEdge = graphModel.CreateEdge(edge.ToPort, newModel.OutputPort);
                    var deletedModels = graphModel.DeleteEdge(edge);
                    state.MarkNew(newEdge);
                    state.MarkDeleted(deletedModels);
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

        public static void DefaultReducer(State state, ToggleLockConstantNodeAction action)
        {
            if (!action.Models.Any())
                return;

            state.PushUndo(action);

            foreach (var constantNodeModel in action.Models)
            {
                constantNodeModel.IsLocked = !constantNodeModel.IsLocked;
            }
            state.MarkChanged(action.Models);
        }
    }

    public class ChangeVariableDeclarationAction : ModelAction<IVariableNodeModel>
    {
        const string k_UndoStringSingular = "Change Variable";

        public readonly IVariableDeclarationModel Variable;

        public ChangeVariableDeclarationAction()
            : base(k_UndoStringSingular) {}

        public ChangeVariableDeclarationAction(IReadOnlyList<IVariableNodeModel> models, IVariableDeclarationModel variable)
            : base(k_UndoStringSingular, k_UndoStringSingular, models)
        {
            Variable = variable;
        }

        public static void DefaultReducer(State state, ChangeVariableDeclarationAction action)
        {
            if (!action.Models.Any())
                return;

            state.PushUndo(action);

            foreach (var model in action.Models)
            {
                model.DeclarationModel = action.Variable;
                model.DefineNode();

                var references = state.GraphModel.FindReferencesInGraph<IVariableNodeModel>(action.Variable);
                state.MarkChanged(references);
            }
            state.MarkChanged(action.Models);
        }
    }
}
