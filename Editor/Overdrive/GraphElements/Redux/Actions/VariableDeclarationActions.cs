using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public class CreateGraphVariableDeclarationAction : BaseAction
    {
        public string VariableName;
        public bool IsExposed;
        public TypeHandle TypeHandle;
        public GUID Guid;
        public ModifierFlags ModifierFlags;

        public CreateGraphVariableDeclarationAction()
        {
            UndoString = "Create Variable";
        }

        public CreateGraphVariableDeclarationAction(string name, bool isExposed, TypeHandle typeHandle,
                                                    ModifierFlags modifierFlags = ModifierFlags.None, GUID? guid = null) : this()
        {
            VariableName = name;
            IsExposed = isExposed;
            TypeHandle = typeHandle;
            Guid = guid ?? GUID.Generate();
            ModifierFlags = modifierFlags;
        }

        public static void DefaultReducer(State state, CreateGraphVariableDeclarationAction action)
        {
            state.PushUndo(action);

            var graphModel = state.GraphModel;
            var newVD = graphModel.CreateGraphVariableDeclaration(action.TypeHandle, action.VariableName,
                action.ModifierFlags, action.IsExposed, null, action.Guid);

            state.MarkNew(newVD);
        }
    }

    public class ReorderGraphVariableDeclarationAction : BaseAction
    {
        public readonly IEnumerable<IVariableDeclarationModel> VariableDeclarationModelsToMove;
        public readonly IVariableDeclarationModel InsertAfter;

        public ReorderGraphVariableDeclarationAction()
        {
            UndoString = "Reorder Variable";
        }

        public ReorderGraphVariableDeclarationAction(IEnumerable<IVariableDeclarationModel> modelsToMove,
                                                     IVariableDeclarationModel insertAfter) : this()
        {
            VariableDeclarationModelsToMove = modelsToMove;
            InsertAfter = insertAfter;
        }

        public static void DefaultReducer(State state, ReorderGraphVariableDeclarationAction action)
        {
            state.PushUndo(action);
            state.GraphModel.MoveAfter(action.VariableDeclarationModelsToMove.ToList(), action.InsertAfter);

            // PF FIXME: this is like a complete rebuild of the blackboard.
            state.MarkChanged(state.BlackboardGraphModel);
        }
    }

    public class InitializeVariableAction : BaseAction
    {
        public IVariableDeclarationModel VariableDeclarationModel;

        public InitializeVariableAction()
        {
            UndoString = "Initialize Variable";
        }

        public InitializeVariableAction(IVariableDeclarationModel variableDeclarationModel)
            : this()
        {
            VariableDeclarationModel = variableDeclarationModel;
        }

        public static void DefaultReducer(State state, InitializeVariableAction action)
        {
            state.PushUndo(action);
            action.VariableDeclarationModel.CreateInitializationValue();

            state.MarkChanged(action.VariableDeclarationModel);
        }
    }

    public class ChangeVariableTypeAction : BaseAction
    {
        public IVariableDeclarationModel VariableDeclarationModel;
        public TypeHandle Handle;

        public ChangeVariableTypeAction()
        {
            UndoString = "Change Variable Type";
        }

        public ChangeVariableTypeAction(IVariableDeclarationModel variableDeclarationModel, TypeHandle handle) : this()
        {
            VariableDeclarationModel = variableDeclarationModel;
            Handle = handle;
        }

        public static void DefaultReducer(State state, ChangeVariableTypeAction action)
        {
            var graphModel = state.GraphModel;

            if (action.Handle.IsValid)
            {
                state.PushUndo(action);

                if (action.VariableDeclarationModel.DataType != action.Handle)
                    action.VariableDeclarationModel.CreateInitializationValue();

                action.VariableDeclarationModel.DataType = action.Handle;

                var variableReferences = graphModel.FindReferencesInGraph<IVariableNodeModel>(action.VariableDeclarationModel).ToList();
                foreach (var usage in variableReferences)
                {
                    usage.UpdateTypeFromDeclaration();
                }

                state.MarkChanged(variableReferences);
                state.MarkChanged(action.VariableDeclarationModel);
            }
        }
    }

    public class UpdateExposedAction : BaseAction
    {
        public IVariableDeclarationModel VariableDeclarationModel;
        public bool Exposed;

        public UpdateExposedAction()
        {
            UndoString = "Change Variable Exposition";
        }

        public UpdateExposedAction(IVariableDeclarationModel variableDeclarationModel, bool exposed) : this()
        {
            VariableDeclarationModel = variableDeclarationModel;
            Exposed = exposed;

            UndoString = Exposed ? "Show Variable" : "Hide Variable";
        }

        public static void DefaultReducer(State state, UpdateExposedAction action)
        {
            state.PushUndo(action);

            action.VariableDeclarationModel.IsExposed = action.Exposed;

            state.MarkChanged(action.VariableDeclarationModel);
        }
    }

    public class UpdateTooltipAction : BaseAction
    {
        public IVariableDeclarationModel VariableDeclarationModel;
        public string Tooltip;

        public UpdateTooltipAction()
        {
            UndoString = "Edit Tooltip";
        }

        public UpdateTooltipAction(IVariableDeclarationModel variableDeclarationModel, string tooltip) : this()
        {
            VariableDeclarationModel = variableDeclarationModel;
            Tooltip = tooltip;
        }

        public static void DefaultReducer(State state, UpdateTooltipAction action)
        {
            state.PushUndo(action);
            action.VariableDeclarationModel.Tooltip = action.Tooltip;

            var graphModel = state.GraphModel;
            var references = graphModel.FindReferencesInGraph<IVariableNodeModel>(action.VariableDeclarationModel);
            state.MarkChanged(references);
            state.MarkChanged(action.VariableDeclarationModel);
        }
    }

    public class ExpandOrCollapseBlackboardRowAction : BaseAction
    {
        public readonly IVariableDeclarationModel Row;
        public readonly bool Expand;

        public ExpandOrCollapseBlackboardRowAction()
        {
            UndoString = "Expand Or Collapse Variable Declaration";
        }

        public ExpandOrCollapseBlackboardRowAction(bool expand, IVariableDeclarationModel row) : this()
        {
            this.Row = row;
            Expand = expand;

            UndoString = Expand ? "Collapse Variable Declaration" : "Expand Variable Declaration";
        }

        public static void DefaultReducer(State state, ExpandOrCollapseBlackboardRowAction action)
        {
            state.PushUndo(action);

            state.BlackboardViewState.SetVariableDeclarationModelExpanded(action.Row, action.Expand);

            state.MarkChanged(action.Row);
        }
    }
}
