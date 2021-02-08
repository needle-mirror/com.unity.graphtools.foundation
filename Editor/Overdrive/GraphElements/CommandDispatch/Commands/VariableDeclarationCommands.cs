using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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
        /// <param name="name">The name of the variable to create.</param>
        /// <param name="isExposed">Whether or not the variable is exposed.</param>
        /// <param name="typeHandle">The type of the variable to create.</param>
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
        /// Default command handler for CreateGraphVariableDeclarationCommand.
        /// </summary>
        /// <param name="graphToolState">The current graph tool state.</param>
        /// <param name="command">The command to handle.</param>
        public static void DefaultCommandHandler(GraphToolState graphToolState, CreateGraphVariableDeclarationCommand command)
        {
            graphToolState.PushUndo(command);

            var graphModel = graphToolState.GraphModel;
            var newVD = graphModel.CreateGraphVariableDeclaration(command.TypeHandle, command.VariableName,
                command.ModifierFlags, command.IsExposed, null, command.Guid);

            graphToolState.MarkNew(newVD);
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
            graphToolState.GraphModel.MoveAfter(command.VariableDeclarationModelsToMove.ToList(), command.InsertAfter);

            // PF FIXME: this is like a complete rebuild of the blackboard.
            graphToolState.MarkChanged(graphToolState.BlackboardGraphModel);
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
            command.VariableDeclarationModel.CreateInitializationValue();

            graphToolState.MarkChanged(command.VariableDeclarationModel);
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
            var graphModel = graphToolState.GraphModel;

            if (command.Handle.IsValid)
            {
                graphToolState.PushUndo(command);

                if (command.VariableDeclarationModel.DataType != command.Handle)
                    command.VariableDeclarationModel.CreateInitializationValue();

                command.VariableDeclarationModel.DataType = command.Handle;

                var variableReferences = graphModel.FindReferencesInGraph<IVariableNodeModel>(command.VariableDeclarationModel).ToList();
                foreach (var usage in variableReferences)
                {
                    usage.UpdateTypeFromDeclaration();
                }

                graphToolState.MarkChanged(variableReferences);
                graphToolState.MarkChanged(command.VariableDeclarationModel);
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

            command.VariableDeclarationModel.IsExposed = command.Exposed;

            graphToolState.MarkChanged(command.VariableDeclarationModel);
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
            command.VariableDeclarationModel.Tooltip = command.Tooltip;

            var graphModel = graphToolState.GraphModel;
            var references = graphModel.FindReferencesInGraph<IVariableNodeModel>(command.VariableDeclarationModel);
            graphToolState.MarkChanged(references);
            graphToolState.MarkChanged(command.VariableDeclarationModel);
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
            this.Row = row;
            Expand = expand;

            UndoString = Expand ? "Collapse Variable Declaration" : "Expand Variable Declaration";
        }

        public static void DefaultCommandHandler(GraphToolState graphToolState, ExpandOrCollapseBlackboardRowCommand command)
        {
            graphToolState.PushUndo(command);

            graphToolState.BlackboardViewState.SetVariableDeclarationModelExpanded(command.Row, command.Expand);

            graphToolState.MarkChanged(command.Row);
        }
    }
}
