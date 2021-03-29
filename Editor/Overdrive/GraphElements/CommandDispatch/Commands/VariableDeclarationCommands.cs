using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Command to create a variable.
    /// </summary>
    public class CreateGraphVariableDeclarationCommand : Command
    {
        /// <summary>
        /// The name of the variable to create.
        /// </summary>
        public string VariableName;

        /// <summary>
        /// Whether or not the variable is exposed.
        /// </summary>
        public bool IsExposed;

        /// <summary>
        /// The type of variable to create.
        /// </summary>
        public Type VariableType;

        /// <summary>
        /// The type of the variable to create.
        /// </summary>
        public TypeHandle TypeHandle;

        /// <summary>
        /// The SerializableGUID to assign to the newly created variable.
        /// </summary>
        public SerializableGUID Guid;

        /// <summary>
        /// The modifiers to apply to the newly created variable.
        /// </summary>
        public ModifierFlags ModifierFlags;

        /// <summary>
        /// Initializes a new CreateGraphVariableDeclarationCommand.
        /// </summary>
        public CreateGraphVariableDeclarationCommand()
        {
            UndoString = "Create Variable";
        }

        /// <summary>
        /// Initializes a new CreateGraphVariableDeclarationCommand.
        /// </summary>
        /// <remarks>This constructor will create the graph's default variable declaration.</remarks>
        /// <param name="name">The name of the variable to create.</param>
        /// <param name="isExposed">Whether or not the variable is exposed.</param>
        /// <param name="typeHandle">The type of data the new variable declaration to create represents.</param>
        /// <param name="modifierFlags">The modifiers to apply to the newly created variable.</param>
        /// <param name="guid">The SerializableGUID to assign to the newly created item. If none is provided, a new
        /// SerializableGUID will be generated for it.</param>
        public CreateGraphVariableDeclarationCommand(string name, bool isExposed, TypeHandle typeHandle,
                                                     ModifierFlags modifierFlags = ModifierFlags.None, SerializableGUID guid = default) : this()
        {
            VariableName = name;
            IsExposed = isExposed;
            TypeHandle = typeHandle;
            Guid = guid.Valid ? guid : SerializableGUID.Generate();
            ModifierFlags = modifierFlags;
        }

        /// <summary>
        /// Initializes a new CreateGraphVariableDeclarationCommand.
        /// </summary>
        /// <param name="name">The name of the variable to create.</param>
        /// <param name="isExposed">Whether or not the variable is exposed.</param>
        /// <param name="typeHandle">The type of data the new variable declaration to create represents.</param>
        /// <param name="variableType">The type of variable declaration to create.</param>
        /// <param name="modifierFlags">The modifiers to apply to the newly created variable.</param>
        /// <param name="guid">The SerializableGUID to assign to the newly created item. If none is provided, a new
        /// SerializableGUID will be generated for it.</param>
        public CreateGraphVariableDeclarationCommand(string name, bool isExposed, TypeHandle typeHandle, Type variableType,
                                                     ModifierFlags modifierFlags = ModifierFlags.None, SerializableGUID guid = default)
            : this(name, isExposed, typeHandle, modifierFlags, guid)
        {
            VariableType = variableType;
        }

        /// <summary>
        /// Default command handler for CreateGraphVariableDeclarationCommand.
        /// </summary>
        /// <param name="graphToolState">The current graph tool state.</param>
        /// <param name="command">The command to handle.</param>
        public static void DefaultCommandHandler(GraphToolState graphToolState, CreateGraphVariableDeclarationCommand command)
        {
            graphToolState.PushUndo(command);

            using (var graphUpdater = graphToolState.GraphViewState.UpdateScope)
            {
                var graphModel = graphToolState.GraphViewState.GraphModel;
                IVariableDeclarationModel newVD;
                if (command.VariableType != null)
                    newVD = graphModel.CreateGraphVariableDeclaration(command.VariableType, command.TypeHandle, command.VariableName,
                        command.ModifierFlags, command.IsExposed, null, command.Guid);
                else
                    newVD = graphModel.CreateGraphVariableDeclaration(command.TypeHandle, command.VariableName,
                        command.ModifierFlags, command.IsExposed, null, command.Guid);

                graphUpdater.MarkNew(newVD);
            }
        }
    }

    public class ReorderGraphVariableDeclarationCommand : Command
    {
        public readonly IEnumerable<IVariableDeclarationModel> VariableDeclarationModelsToMove;
        public readonly IVariableDeclarationModel InsertAfter;

        public ReorderGraphVariableDeclarationCommand()
        {
            UndoString = "Reorder Variable";
        }

        public ReorderGraphVariableDeclarationCommand(IEnumerable<IVariableDeclarationModel> modelsToMove,
                                                      IVariableDeclarationModel insertAfter) : this()
        {
            VariableDeclarationModelsToMove = modelsToMove;
            InsertAfter = insertAfter;
        }

        public static void DefaultCommandHandler(GraphToolState graphToolState, ReorderGraphVariableDeclarationCommand command)
        {
            graphToolState.PushUndo(command);

            using (var graphUpdater = graphToolState.GraphViewState.UpdateScope)
            {
                graphToolState.GraphViewState.GraphModel.MoveAfter(command.VariableDeclarationModelsToMove.ToList(), command.InsertAfter);

                // Since potentially the index of every VD changed, let's mark them all as changed.
                graphUpdater.MarkChanged(graphToolState.GraphViewState.GraphModel.VariableDeclarations);
            }
        }
    }

    public class InitializeVariableCommand : Command
    {
        public IVariableDeclarationModel VariableDeclarationModel;

        public InitializeVariableCommand()
        {
            UndoString = "Initialize Variable";
        }

        public InitializeVariableCommand(IVariableDeclarationModel variableDeclarationModel)
            : this()
        {
            VariableDeclarationModel = variableDeclarationModel;
        }

        public static void DefaultCommandHandler(GraphToolState graphToolState, InitializeVariableCommand command)
        {
            graphToolState.PushUndo(command);

            using (var graphUpdater = graphToolState.GraphViewState.UpdateScope)
            {
                command.VariableDeclarationModel.CreateInitializationValue();
                graphUpdater.MarkChanged(command.VariableDeclarationModel);
            }
        }
    }

    public class ChangeVariableTypeCommand : Command
    {
        public IVariableDeclarationModel VariableDeclarationModel;
        public TypeHandle Handle;

        public ChangeVariableTypeCommand()
        {
            UndoString = "Change Variable Type";
        }

        public ChangeVariableTypeCommand(IVariableDeclarationModel variableDeclarationModel, TypeHandle handle) : this()
        {
            VariableDeclarationModel = variableDeclarationModel;
            Handle = handle;
        }

        public static void DefaultCommandHandler(GraphToolState graphToolState, ChangeVariableTypeCommand command)
        {
            var graphModel = graphToolState.GraphViewState.GraphModel;

            if (command.Handle.IsValid)
            {
                graphToolState.PushUndo(command);

                using (var graphUpdater = graphToolState.GraphViewState.UpdateScope)
                {
                    if (command.VariableDeclarationModel.DataType != command.Handle)
                        command.VariableDeclarationModel.CreateInitializationValue();

                    command.VariableDeclarationModel.DataType = command.Handle;

                    var variableReferences = graphModel.FindReferencesInGraph<IVariableNodeModel>(command.VariableDeclarationModel).ToList();
                    foreach (var usage in variableReferences)
                    {
                        usage.UpdateTypeFromDeclaration();
                    }

                    graphUpdater.MarkChanged(variableReferences);
                    graphUpdater.MarkChanged(command.VariableDeclarationModel);
                }
            }
        }
    }

    public class UpdateExposedCommand : Command
    {
        public IVariableDeclarationModel VariableDeclarationModel;
        public bool Exposed;

        public UpdateExposedCommand()
        {
            UndoString = "Change Variable Exposition";
        }

        public UpdateExposedCommand(IVariableDeclarationModel variableDeclarationModel, bool exposed) : this()
        {
            VariableDeclarationModel = variableDeclarationModel;
            Exposed = exposed;

            UndoString = Exposed ? "Show Variable" : "Hide Variable";
        }

        public static void DefaultCommandHandler(GraphToolState graphToolState, UpdateExposedCommand command)
        {
            graphToolState.PushUndo(command);

            using (var graphUpdater = graphToolState.GraphViewState.UpdateScope)
            {
                command.VariableDeclarationModel.IsExposed = command.Exposed;
                graphUpdater.MarkChanged(command.VariableDeclarationModel);
            }
        }
    }

    public class UpdateTooltipCommand : Command
    {
        public IVariableDeclarationModel VariableDeclarationModel;
        public string Tooltip;

        public UpdateTooltipCommand()
        {
            UndoString = "Edit Tooltip";
        }

        public UpdateTooltipCommand(IVariableDeclarationModel variableDeclarationModel, string tooltip) : this()
        {
            VariableDeclarationModel = variableDeclarationModel;
            Tooltip = tooltip;
        }

        public static void DefaultCommandHandler(GraphToolState graphToolState, UpdateTooltipCommand command)
        {
            graphToolState.PushUndo(command);

            using (var graphUpdater = graphToolState.GraphViewState.UpdateScope)
            {
                command.VariableDeclarationModel.Tooltip = command.Tooltip;

                var graphModel = graphToolState.GraphViewState.GraphModel;
                var references = graphModel.FindReferencesInGraph<IVariableNodeModel>(command.VariableDeclarationModel);
                graphUpdater.MarkChanged(references);
                graphUpdater.MarkChanged(command.VariableDeclarationModel);
            }
        }
    }

    public class ExpandOrCollapseBlackboardRowCommand : Command
    {
        public readonly IVariableDeclarationModel Row;
        public readonly bool Expand;

        public ExpandOrCollapseBlackboardRowCommand()
        {
            UndoString = "Expand Or Collapse Variable Declaration";
        }

        public ExpandOrCollapseBlackboardRowCommand(bool expand, IVariableDeclarationModel row) : this()
        {
            Row = row;
            Expand = expand;

            UndoString = Expand ? "Collapse Variable Declaration" : "Expand Variable Declaration";
        }

        public static void DefaultCommandHandler(GraphToolState graphToolState, ExpandOrCollapseBlackboardRowCommand command)
        {
            graphToolState.PushUndo(command);

            using (var bbUpdater = graphToolState.BlackboardViewState.UpdateScope)
            {
                bbUpdater.SetVariableDeclarationModelExpanded(command.Row, command.Expand);
            }
        }
    }
}
