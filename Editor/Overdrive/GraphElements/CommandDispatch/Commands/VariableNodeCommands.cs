using System.Collections.Generic;
using System.Linq;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public class CreateVariableNodesCommand : UndoableCommand
    {
        public List<(IVariableDeclarationModel, SerializableGUID, Vector2)> VariablesToCreate;
        public IPortModel ConnectAfterCreation;
        public IReadOnlyList<IEdgeModel> EdgeModelsToDelete;
        public bool AutoAlign;

        public CreateVariableNodesCommand()
        {
            UndoString = "Create Variable Node";
        }

        public CreateVariableNodesCommand(IReadOnlyList<(IVariableDeclarationModel, SerializableGUID, Vector2)> variablesToCreate) : this()
        {
            VariablesToCreate = variablesToCreate?.ToList() ?? new List<(IVariableDeclarationModel, SerializableGUID, Vector2)>();
        }

        public CreateVariableNodesCommand(IVariableDeclarationModel graphElementModel, Vector2 mousePosition,
                                          IReadOnlyList<IEdgeModel> edgeModelsToDelete = null, IPortModel connectAfterCreation = null,
                                          bool autoAlign = false) : this()
        {
            VariablesToCreate = new List<(IVariableDeclarationModel, SerializableGUID, Vector2)>
            {
                (graphElementModel, SerializableGUID.Generate(), mousePosition)
            };
            EdgeModelsToDelete = edgeModelsToDelete;
            ConnectAfterCreation = connectAfterCreation;
            AutoAlign = autoAlign;
        }

        public static void DefaultCommandHandler(GraphToolState graphToolState, CreateVariableNodesCommand command)
        {
            if (command.VariablesToCreate.Count > 0)
            {
                graphToolState.PushUndo(command);

                using (var graphUpdater = graphToolState.GraphViewState.UpdateScope)
                {
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
                        graphToolState.GraphViewState.GraphModel.DeleteEdges(edgesToDelete);
                        graphUpdater.MarkDeleted(edgesToDelete);
                    }

                    foreach (var (variableDeclarationModel, guid, position) in command.VariablesToCreate)
                    {
                        var vsGraphModel = graphToolState.GraphViewState.GraphModel;

                        var newVariable = vsGraphModel.CreateVariableNode(variableDeclarationModel, position, guid: guid);
                        graphUpdater.MarkNew(newVariable);

                        if (portToConnect != null)
                        {
                            var newEdge =
                                graphToolState.GraphViewState.GraphModel.CreateEdge(portToConnect, newVariable.OutputPort);
                            graphUpdater.MarkNew(newEdge);
                            if (command.AutoAlign)
                            {
                                graphUpdater.MarkModelToAutoAlign(newEdge);
                            }
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// A command to convert variables to constants and vice versa.
    /// </summary>
    public class ConvertConstantNodesAndVariableNodesCommand : UndoableCommand
    {
        public IReadOnlyList<IConstantNodeModel> ConstantModels;
        public IReadOnlyList<IVariableNodeModel> VariableModels;

        const string k_UndoString = "Convert Constants And Variables";
        const string k_UndoStringCToVSingular = "Convert Constant To Variable";
        const string k_UndoStringCToVPlural = "Convert Constants To Variables";
        const string k_UndoStringVToCSingular = "Convert Variable To Constant";
        const string k_UndoStringVToCPlural = "Convert Variables To Constants";

        /// <summary>
        /// Initializes a new instance of the ConvertConstantNodesAndVariableNodesCommand class.
        /// </summary>
        public ConvertConstantNodesAndVariableNodesCommand()
        {
            UndoString = k_UndoString;
        }

        /// <summary>
        /// Initializes a new instance of the ConvertConstantNodesAndVariableNodesCommand class.
        /// </summary>
        /// <param name="constantModels">The constants to convert to variables.</param>
        /// <param name="variableModels">The variables to convert to constants.</param>
        public ConvertConstantNodesAndVariableNodesCommand(
            IReadOnlyList<IConstantNodeModel> constantModels,
            IReadOnlyList<IVariableNodeModel> variableModels)
        {
            ConstantModels = constantModels;
            VariableModels = variableModels;

            var constantCount = ConstantModels?.Count ?? 0;
            var variableCount = VariableModels?.Count ?? 0;

            if (constantCount == 0)
            {
                if (variableCount == 1)
                {
                    UndoString = k_UndoStringVToCSingular;
                }
                else
                {
                    UndoString = k_UndoStringVToCPlural;
                }
            }
            else if (variableCount == 0)
            {
                if (constantCount == 1)
                {
                    UndoString = k_UndoStringCToVSingular;
                }
                else
                {
                    UndoString = k_UndoStringCToVPlural;
                }
            }
            else
            {
                UndoString = k_UndoString;
            }
        }

        /// <summary>
        /// Default command handler.
        /// </summary>
        /// <param name="graphToolState">The state to modify.</param>
        /// <param name="command">The command to apply to the state.</param>
        public static void DefaultCommandHandler(GraphToolState graphToolState, ConvertConstantNodesAndVariableNodesCommand command)
        {
            if ((command.ConstantModels?.Count ?? 0) == 0 && (command.VariableModels?.Count ?? 0) == 0)
                return;

            graphToolState.PushUndo(command);

            using (var graphUpdater = graphToolState.GraphViewState.UpdateScope)
            using (var selectionUpdater = graphToolState.SelectionState.UpdateScope)
            {
                var graphModel = graphToolState.GraphViewState.GraphModel;

                foreach (var constantModel in command.ConstantModels ?? Enumerable.Empty<IConstantNodeModel>())
                {
                    var declarationModel = graphModel.CreateGraphVariableDeclaration(
                        constantModel.Type.GenerateTypeHandle(),
                        constantModel.Type.FriendlyName().CodifyString(), ModifierFlags.None, true,
                        constantModel.Value.CloneConstant());
                    graphUpdater.MarkNew(declarationModel);

                    if (graphModel.CreateVariableNode(declarationModel, constantModel.Position) is IVariableNodeModel variableModel)
                    {
                        graphUpdater.MarkNew(variableModel);
                        selectionUpdater.SelectElement(variableModel, true);

                        variableModel.State = constantModel.State;
                        if (constantModel.HasUserColor)
                            variableModel.Color = constantModel.Color;
                        foreach (var edge in graphModel.GetEdgesConnections(constantModel.OutputPort).ToList())
                        {
                            var newEdge = graphModel.CreateEdge(edge.ToPort, variableModel.OutputPort);
                            var deletedModels = graphModel.DeleteEdge(edge);

                            graphUpdater.MarkNew(newEdge);
                            graphUpdater.MarkDeleted(deletedModels);
                            selectionUpdater.SelectElements(deletedModels, false);
                        }
                    }

                    var deletedElements = graphModel.DeleteNode(constantModel, deleteConnections: false);
                    graphUpdater.MarkDeleted(deletedElements);
                    selectionUpdater.SelectElements(deletedElements, false);
                }

                foreach (var variableModel in command.VariableModels ?? Enumerable.Empty<IVariableNodeModel>())
                {
                    if (graphModel.Stencil.GetConstantNodeValueType(variableModel.GetDataType()) == null)
                        continue;
                    var constantModel = graphModel.CreateConstantNode(variableModel.GetDataType(), variableModel.Title, variableModel.Position);
                    constantModel.ObjectValue = variableModel.VariableDeclarationModel?.InitializationModel?.ObjectValue;
                    constantModel.State = variableModel.State;
                    if (variableModel.HasUserColor)
                        constantModel.Color = variableModel.Color;
                    graphUpdater.MarkNew(constantModel);
                    selectionUpdater.SelectElement(constantModel, true);

                    var edgeModels = graphModel.GetEdgesConnections(variableModel.OutputPort).ToList();
                    foreach (var edge in edgeModels)
                    {
                        var newEdge = graphModel.CreateEdge(edge.ToPort, constantModel.OutputPort);
                        var deletedModels = graphModel.DeleteEdge(edge);
                        graphUpdater.MarkNew(newEdge);
                        graphUpdater.MarkDeleted(deletedModels);
                        selectionUpdater.SelectElements(deletedModels, false);
                    }

                    var deletedElements = graphModel.DeleteNode(variableModel, deleteConnections: false);
                    graphUpdater.MarkDeleted(deletedElements);
                    selectionUpdater.SelectElements(deletedElements, false);
                }
            }
        }
    }

    // Create a separate instance of the constant or variable node for each connections on the original variable node.
    public class ItemizeNodeCommand : ModelCommand<ISingleOutputPortNodeModel>
    {
        const string k_UndoStringSingular = "Itemize Node";
        const string k_UndoStringPlural = "Itemize Nodes";

        public ItemizeNodeCommand()
            : base(k_UndoStringSingular) { }

        public ItemizeNodeCommand(params ISingleOutputPortNodeModel[] models)
            : base(k_UndoStringSingular, k_UndoStringPlural, models) { }

        public static void DefaultCommandHandler(GraphToolState graphToolState, ItemizeNodeCommand command)
        {
            bool undoPushed = false;

            using (var graphUpdater = graphToolState.GraphViewState.UpdateScope)
            {
                var graphModel = graphToolState.GraphViewState.GraphModel;
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

                        var newModel = (ISingleOutputPortNodeModel)graphModel.DuplicateNode(model, i * 50 * Vector2.up);
                        graphUpdater.MarkNew(newModel);
                        var edge = edges[i];
                        var newEdge = graphModel.CreateEdge(edge.ToPort, newModel.OutputPort);
                        var deletedModels = graphModel.DeleteEdge(edge);
                        graphUpdater.MarkNew(newEdge);
                        graphUpdater.MarkDeleted(deletedModels);
                    }
                }
            }
        }
    }

    public class ToggleLockConstantNodeCommand : ModelCommand<IConstantNodeModel>
    {
        const string k_UndoStringSingular = "Toggle Lock Constant";
        const string k_UndoStringPlural = "Toggle Lock Constants";

        public ToggleLockConstantNodeCommand()
            : base(k_UndoStringSingular) { }

        public ToggleLockConstantNodeCommand(params IConstantNodeModel[] constantNodeModels)
            : base(k_UndoStringSingular, k_UndoStringPlural, constantNodeModels) { }

        public static void DefaultCommandHandler(GraphToolState graphToolState, ToggleLockConstantNodeCommand command)
        {
            if (!command.Models.Any())
                return;

            graphToolState.PushUndo(command);

            using (var graphUpdater = graphToolState.GraphViewState.UpdateScope)
            {
                foreach (var constantNodeModel in command.Models)
                {
                    constantNodeModel.IsLocked = !constantNodeModel.IsLocked;
                }

                graphUpdater.MarkChanged(command.Models);
            }
        }
    }

    public class ChangeVariableDeclarationCommand : ModelCommand<IVariableNodeModel>
    {
        const string k_UndoStringSingular = "Change Variable";

        public readonly IVariableDeclarationModel Variable;

        public ChangeVariableDeclarationCommand()
            : base(k_UndoStringSingular) { }

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

            using (var graphUpdater = graphToolState.GraphViewState.UpdateScope)
            {
                foreach (var model in command.Models)
                {
                    model.DeclarationModel = command.Variable;

                    var references = graphToolState.GraphViewState.GraphModel.FindReferencesInGraph<IVariableNodeModel>(command.Variable);
                    graphUpdater.MarkChanged(references);
                }

                graphUpdater.MarkChanged(command.Models);
            }
        }
    }
}
