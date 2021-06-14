using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Command to create a variable.
    /// </summary>
    public class CreateGraphVariableDeclarationCommand : UndoableCommand
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

    /// <summary>
    /// Command to reorder variables.
    /// </summary>
    public class ReorderGraphVariableDeclarationCommand : UndoableCommand
    {
        /// <summary>
        /// The variables to move.
        /// </summary>
        public readonly IReadOnlyList<IVariableDeclarationModel> VariableDeclarationModels;
        /// <summary>
        /// The variable after which the moved variables should be inserted.
        /// </summary>
        public readonly IVariableDeclarationModel InsertAfter;

        /// <summary>
        /// Initializes a new instance of the <see cref="ReorderGraphVariableDeclarationCommand"/> class.
        /// </summary>
        public ReorderGraphVariableDeclarationCommand()
        {
            UndoString = "Reorder Variable";
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ReorderGraphVariableDeclarationCommand"/> class.
        /// </summary>
        /// <param name="insertAfter">The variable after which the moved variables should be inserted.</param>
        /// <param name="variableDeclarationModels">The variables to move.</param>
        public ReorderGraphVariableDeclarationCommand(IVariableDeclarationModel insertAfter,
            IReadOnlyList<IVariableDeclarationModel> variableDeclarationModels) : this()
        {
            VariableDeclarationModels = variableDeclarationModels;
            InsertAfter = insertAfter;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ReorderGraphVariableDeclarationCommand"/> class.
        /// </summary>
        /// <param name="insertAfter">The variable after which the moved variables should be inserted.</param>
        /// <param name="variableDeclarationModels">The variables to move.</param>
        public ReorderGraphVariableDeclarationCommand(IVariableDeclarationModel insertAfter,
            params IVariableDeclarationModel[] variableDeclarationModels)
            : this(insertAfter, (IReadOnlyList<IVariableDeclarationModel>)variableDeclarationModels)
        {
        }

        /// <summary>
        /// Default command handler.
        /// </summary>
        /// <param name="graphToolState">The state.</param>
        /// <param name="command">The command.</param>
        public static void DefaultCommandHandler(GraphToolState graphToolState, ReorderGraphVariableDeclarationCommand command)
        {
            graphToolState.PushUndo(command);

            using (var graphUpdater = graphToolState.GraphViewState.UpdateScope)
            {
                graphToolState.GraphViewState.GraphModel.MoveAfter(command.VariableDeclarationModels, command.InsertAfter);

                // Since potentially the index of every VD changed, let's mark them all as changed.
                graphUpdater.MarkChanged(graphToolState.GraphViewState.GraphModel.VariableDeclarations);
            }
        }
    }

    /// <summary>
    /// Command to create the initialization value of a variable.
    /// </summary>
    public class InitializeVariableCommand : UndoableCommand
    {
        /// <summary>
        /// The variable to initialize.
        /// </summary>
        public IVariableDeclarationModel VariableDeclarationModel;

        /// <summary>
        /// Initializes a new instance of the <see cref="InitializeVariableCommand"/> class.
        /// </summary>
        public InitializeVariableCommand()
        {
            UndoString = "Initialize Variable";
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InitializeVariableCommand"/> class.
        /// </summary>
        /// <param name="variableDeclarationModel">The variable to initialize.</param>
        public InitializeVariableCommand(IVariableDeclarationModel variableDeclarationModel)
            : this()
        {
            VariableDeclarationModel = variableDeclarationModel;
        }

        /// <summary>
        /// Default command handler.
        /// </summary>
        /// <param name="graphToolState">The state.</param>
        /// <param name="command">The command.</param>
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

    /// <summary>
    /// Command to change the type of a variable.
    /// </summary>
    public class ChangeVariableTypeCommand : UndoableCommand
    {
        /// <summary>
        /// The variable to update.
        /// </summary>
        public IVariableDeclarationModel VariableDeclarationModel;
        /// <summary>
        /// The new variable type.
        /// </summary>
        public TypeHandle Type;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChangeVariableTypeCommand"/> class.
        /// </summary>
        public ChangeVariableTypeCommand()
        {
            UndoString = "Change Variable Type";
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ChangeVariableTypeCommand"/> class.
        /// </summary>
        /// <param name="variableDeclarationModel">The variable to update.</param>
        /// <param name="type">The new variable type.</param>
        public ChangeVariableTypeCommand(IVariableDeclarationModel variableDeclarationModel, TypeHandle type) : this()
        {
            VariableDeclarationModel = variableDeclarationModel;
            Type = type;
        }

        /// <summary>
        /// Default command handler.
        /// </summary>
        /// <param name="graphToolState">The state.</param>
        /// <param name="command">The command.</param>
        public static void DefaultCommandHandler(GraphToolState graphToolState, ChangeVariableTypeCommand command)
        {
            var graphModel = graphToolState.GraphViewState.GraphModel;

            if (command.Type.IsValid)
            {
                graphToolState.PushUndo(command);

                using (var graphUpdater = graphToolState.GraphViewState.UpdateScope)
                {
                    if (command.VariableDeclarationModel.DataType != command.Type)
                        command.VariableDeclarationModel.CreateInitializationValue();

                    command.VariableDeclarationModel.DataType = command.Type;

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

    /// <summary>
    /// Command to change the Exposed value of a variable.
    /// </summary>
    public class ExposeVariableCommand : UndoableCommand
    {
        /// <summary>
        /// The variable to update.
        /// </summary>
        public IVariableDeclarationModel VariableDeclarationModel;
        /// <summary>
        /// Whether the variable should be exposed.
        /// </summary>
        public bool Exposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExposeVariableCommand"/> class.
        /// </summary>
        public ExposeVariableCommand()
        {
            UndoString = "Change Variable Exposition";
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExposeVariableCommand"/> class.
        /// </summary>
        /// <param name="variableDeclarationModel">The variable to update.</param>
        /// <param name="exposed">Whether the variable should be exposed.</param>
        public ExposeVariableCommand(IVariableDeclarationModel variableDeclarationModel, bool exposed) : this()
        {
            VariableDeclarationModel = variableDeclarationModel;
            Exposed = exposed;

            UndoString = Exposed ? "Show Variable" : "Hide Variable";
        }

        /// <summary>
        /// Default command handler.
        /// </summary>
        /// <param name="graphToolState">The state.</param>
        /// <param name="command">The command.</param>
        public static void DefaultCommandHandler(GraphToolState graphToolState, ExposeVariableCommand command)
        {
            graphToolState.PushUndo(command);

            using (var graphUpdater = graphToolState.GraphViewState.UpdateScope)
            {
                command.VariableDeclarationModel.IsExposed = command.Exposed;
                graphUpdater.MarkChanged(command.VariableDeclarationModel);
            }
        }
    }

    /// <summary>
    /// Command to update the tooltip of a variable.
    /// </summary>
    public class UpdateTooltipCommand : UndoableCommand
    {
        /// <summary>
        /// The variable to update.
        /// </summary>
        public IVariableDeclarationModel VariableDeclarationModel;
        /// <summary>
        /// The new tooltip for the variable.
        /// </summary>
        public string Tooltip;

        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateTooltipCommand"/> class.
        /// </summary>
        public UpdateTooltipCommand()
        {
            UndoString = "Edit Tooltip";
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateTooltipCommand"/> class.
        /// </summary>
        /// <param name="variableDeclarationModel">The variable to update.</param>
        /// <param name="tooltip">The new tooltip for the variable.</param>
        public UpdateTooltipCommand(IVariableDeclarationModel variableDeclarationModel, string tooltip) : this()
        {
            VariableDeclarationModel = variableDeclarationModel;
            Tooltip = tooltip;
        }

        /// <summary>
        /// Default command handler.
        /// </summary>
        /// <param name="graphToolState">The state.</param>
        /// <param name="command">The command.</param>
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

    /// <summary>
    /// Command to expand or collapse variable in the blackboard.
    /// </summary>
    public class CollapseVariableInBlackboard : UndoableCommand
    {
        /// <summary>
        /// The variable to expand or collapse in the blackboard.
        /// </summary>
        public readonly IVariableDeclarationModel VariableDeclarationModel;
        /// <summary>
        /// Whether to collapse the variable row.
        /// </summary>
        public readonly bool Collapse;

        /// <summary>
        /// Initializes a new instance of the <see cref="CollapseVariableInBlackboard"/> class.
        /// </summary>
        public CollapseVariableInBlackboard()
        {
            UndoString = "Expand Or Collapse Variable Declaration";
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CollapseVariableInBlackboard"/> class.
        /// </summary>
        /// <param name="variableDeclarationModel">The variable to update.</param>
        /// <param name="collapse">Whether to collapse the variable row in the blackboard.</param>
        public CollapseVariableInBlackboard(IVariableDeclarationModel variableDeclarationModel, bool collapse) : this()
        {
            VariableDeclarationModel = variableDeclarationModel;
            Collapse = collapse;

            UndoString = Collapse ? "Collapse Variable Declaration" : "Expand Variable Declaration";
        }

        /// <summary>
        /// Default command handler.
        /// </summary>
        /// <param name="graphToolState">The state.</param>
        /// <param name="command">The command.</param>
        public static void DefaultCommandHandler(GraphToolState graphToolState, CollapseVariableInBlackboard command)
        {
            graphToolState.PushUndo(command);

            using (var bbUpdater = graphToolState.BlackboardViewState.UpdateScope)
            {
                bbUpdater.SetVariableDeclarationModelExpanded(command.VariableDeclarationModel, !command.Collapse);
            }
        }
    }
}
