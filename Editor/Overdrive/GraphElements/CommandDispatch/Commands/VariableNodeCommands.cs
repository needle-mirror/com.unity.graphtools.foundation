using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public class CreateVariableNodesCommand : Command
    {
        public List<(IVariableDeclarationModel, SerializableGUID, Vector2)> VariablesToCreate;
        public IPortModel ConnectAfterCreation;
        public IReadOnlyList<IEdgeModel> EdgeModelsToDelete;
        public bool AutoAlign;

        public CreateVariableNodesCommand()
        {
            UndoString = "Create Variable Node";
        }

        public CreateVariableNodesCommand(List<(IVariableDeclarationModel, SerializableGUID, Vector2)> variablesToCreate) : this()
        {
            VariablesToCreate = variablesToCreate;
        }

        public CreateVariableNodesCommand(IVariableDeclarationModel graphElementModel, Vector2 mousePosition,
                                          IReadOnlyList<IEdgeModel> edgeModelsToDelete = null, IPortModel connectAfterCreation = null,
                                          bool autoAlign = false) : this()
        {
            VariablesToCreate = new List<(IVariableDeclarationModel, SerializableGUID, Vector2)>();
            VariablesToCreate.Add((graphElementModel, SerializableGUID.Generate(), mousePosition));
            EdgeModelsToDelete = edgeModelsToDelete;
            ConnectAfterCreation = connectAfterCreation;
            AutoAlign = autoAlign;
        }

        public static void DefaultCommandHandler(GraphToolState graphToolState, CreateVariableNodesCommand command)
        {
            if (command.VariablesToCreate.Count > 0)
            {
                graphToolState.PushUndo(command);

                var edgesToDelete = command.EdgeModelsToDelete ?? new List<IEdgeModel>();

                // Delete previous connections
                var portToConnect = command.ConnectAfterCreation;
                if (portToConnect != null && portToConnect.Capacity != PortCapacity.Multi)
                {
                    var existingEdges = portToConnect.GetConnectedEdges();
                    edgesToDelete = edgesToDelete.Concat(existingEdges).ToList();
                }

                // Delete previous connections
                if (edgesToDelete.Any())
                {
                    graphToolState.GraphModel.DeleteEdges(edgesToDelete);
                    graphToolState.MarkDeleted(edgesToDelete);
                }

                foreach (var(variableDeclarationModel, guid, position) in command.VariablesToCreate)
                {
                    var vsGraphModel = graphToolState.GraphModel;

                    var newVariable = vsGraphModel.CreateVariableNode(variableDeclarationModel, position, guid: guid);
                    graphToolState.MarkNew(newVariable);

                    if (portToConnect != null)
                    {
                        var newEdge =
                            graphToolState.GraphModel.CreateEdge(portToConnect, newVariable.OutputPort);
                        graphToolState.MarkNew(newEdge);
                        if (command.AutoAlign)
                        {
                            graphToolState.MarkModelToAutoAlign(newEdge);
                        }
                    }
                }
            }
        }
    }

    public class ConvertVariableNodesToConstantNodesCommand : ModelCommand<IVariableNodeModel>
    {
        const string k_UndoStringSingular = "Convert Variable To Constant";
        const string k_UndoStringPlural = "Convert Variables To Constants";

        public ConvertVariableNodesToConstantNodesCommand()
            : base(k_UndoStringSingular) {}

        public ConvertVariableNodesToConstantNodesCommand(params IVariableNodeModel[] variableModels)
            : base(k_UndoStringSingular, k_UndoStringPlural, variableModels) {}

        public static void DefaultCommandHandler(GraphToolState graphToolState, ConvertVariableNodesToConstantNodesCommand command)
        {
            if (!command.Models.Any())
                return;

            graphToolState.PushUndo(command);

            var graphModel = graphToolState.GraphModel;
            foreach (var variableModel in command.Models)
            {
                if (graphModel.Stencil.GetConstantNodeValueType(variableModel.GetDataType()) == null)
                    continue;
                var constantModel = graphModel.CreateConstantNode(variableModel.GetDataType(), variableModel.Title, variableModel.Position);
                constantModel.ObjectValue = variableModel.VariableDeclarationModel?.InitializationModel?.ObjectValue;
                constantModel.State = variableModel.State;
                constantModel.HasUserColor = variableModel.HasUserColor;
                if (variableModel.HasUserColor)
                    constantModel.Color = variableModel.Color;
                graphToolState.MarkNew(constantModel);

                var edgeModels = graphModel.GetEdgesConnections(variableModel.OutputPort).ToList();
                foreach (var edge in edgeModels)
                {
                    var newEdge = graphModel.CreateEdge(edge.ToPort, constantModel.OutputPort);
                    var deletedModels = graphModel.DeleteEdge(edge);
                    graphToolState.MarkNew(newEdge);
                    graphToolState.MarkDeleted(deletedModels);
                }

                var deletedElements = graphModel.DeleteNode(variableModel, deleteConnections: false);
                graphToolState.MarkDeleted(deletedElements);
            }
        }
    }

    public class ConvertConstantNodesToVariableNodesCommand : ModelCommand<IConstantNodeModel>
    {
        const string k_UndoStringSingular = "Convert Constant To Variable";
        const string k_UndoStringPlural = "Convert Constants To Variables";

        public ConvertConstantNodesToVariableNodesCommand()
            : base(k_UndoStringSingular) {}

        public ConvertConstantNodesToVariableNodesCommand(params IConstantNodeModel[] constantModels)
            : base(k_UndoStringSingular, k_UndoStringPlural, constantModels) {}

        public static void DefaultCommandHandler(GraphToolState graphToolState, ConvertConstantNodesToVariableNodesCommand command)
        {
            if (!command.Models.Any())
                return;

            graphToolState.PushUndo(command);

            var graphModel = graphToolState.GraphModel;
            foreach (var constantModel in command.Models)
            {
                var declarationModel = graphModel.CreateGraphVariableDeclaration(
                    constantModel.Type.GenerateTypeHandle(),
                    StringExtensions.CodifyString(constantModel.Type.FriendlyName()), ModifierFlags.None, true,
                    constantModel.Value.CloneConstant());
                graphToolState.MarkNew(declarationModel);

                if (graphModel.CreateVariableNode(declarationModel, constantModel.Position) is IVariableNodeModel variableModel)
                {
                    graphToolState.MarkNew(variableModel);

                    variableModel.State = constantModel.State;
                    variableModel.HasUserColor = constantModel.HasUserColor;
                    if (constantModel.HasUserColor)
                        variableModel.Color = constantModel.Color;
                    foreach (var edge in graphModel.GetEdgesConnections(constantModel.OutputPort).ToList())
                    {
                        var newEdge = graphModel.CreateEdge(edge.ToPort, variableModel.OutputPort);
                        var deleteModels = graphModel.DeleteEdge(edge);

                        graphToolState.MarkNew(newEdge);
                        graphToolState.MarkDeleted(deleteModels);
                    }
                }

                var deletedElements  = graphModel.DeleteNode(constantModel, deleteConnections: false);
                graphToolState.MarkDeleted(deletedElements);
            }
        }
    }

    // Create a separate instance of the constant or variable node for each connections on the original variable node.
    public class ItemizeNodeCommand : ModelCommand<ISingleOutputPortNode>
    {
        const string k_UndoStringSingular = "Itemize Node";
        const string k_UndoStringPlural = "Itemize Nodes";

        public ItemizeNodeCommand()
            : base(k_UndoStringSingular) {}

        public ItemizeNodeCommand(params ISingleOutputPortNode[] models)
            : base(k_UndoStringSingular, k_UndoStringPlural, models) {}

        public static void DefaultCommandHandler(GraphToolState graphToolState, ItemizeNodeCommand command)
        {
            bool undoPushed = false;

            var graphModel = graphToolState.GraphModel;
            foreach (var model in command.Models.Where(m => m is IVariableNodeModel || m is IConstantNodeModel))
            {
                var edges = graphModel.GetEdgesConnections(model.OutputPort).ToList();

                for (var i = 1; i < edges.Count; i++)
                {
                    if (!undoPushed)
                    {
                        undoPushed = true;
                        graphToolState.PushUndo(command);
                    }

                    var newModel = (ISingleOutputPortNode)graphModel.DuplicateNode(model, i * 50 * Vector2.up);
                    graphToolState.MarkNew(newModel);
                    var edge = edges[i];
                    var newEdge = graphModel.CreateEdge(edge.ToPort, newModel.OutputPort);
                    var deletedModels = graphModel.DeleteEdge(edge);
                    graphToolState.MarkNew(newEdge);
                    graphToolState.MarkDeleted(deletedModels);
                }
            }
        }
    }

    public class ToggleLockConstantNodeCommand : ModelCommand<IConstantNodeModel>
    {
        const string k_UndoStringSingular = "Toggle Lock Constant";
        const string k_UndoStringPlural = "Toggle Lock Constants";

        public ToggleLockConstantNodeCommand()
            : base(k_UndoStringSingular) {}

        public ToggleLockConstantNodeCommand(params IConstantNodeModel[] constantNodeModels)
            : base(k_UndoStringSingular, k_UndoStringPlural, constantNodeModels) {}

        public static void DefaultCommandHandler(GraphToolState graphToolState, ToggleLockConstantNodeCommand command)
        {
            if (!command.Models.Any())
                return;

            graphToolState.PushUndo(command);

            foreach (var constantNodeModel in command.Models)
            {
                constantNodeModel.IsLocked = !constantNodeModel.IsLocked;
            }
            graphToolState.MarkChanged(command.Models);
        }
    }

    public class ChangeVariableDeclarationCommand : ModelCommand<IVariableNodeModel>
    {
        const string k_UndoStringSingular = "Change Variable";

        public readonly IVariableDeclarationModel Variable;

        public ChangeVariableDeclarationCommand()
            : base(k_UndoStringSingular) {}

        public ChangeVariableDeclarationCommand(IReadOnlyList<IVariableNodeModel> models, IVariableDeclarationModel variable)
            : base(k_UndoStringSingular, k_UndoStringSingular, models)
        {
            Variable = variable;
        }

        public static void DefaultCommandHandler(GraphToolState graphToolState, ChangeVariableDeclarationCommand command)
        {
            if (!command.Models.Any())
                return;

            graphToolState.PushUndo(command);

            foreach (var model in command.Models)
            {
                model.DeclarationModel = command.Variable;

                var references = graphToolState.GraphModel.FindReferencesInGraph<IVariableNodeModel>(command.Variable);
                graphToolState.MarkChanged(references);
            }
            graphToolState.MarkChanged(command.Models);
        }
    }
}
