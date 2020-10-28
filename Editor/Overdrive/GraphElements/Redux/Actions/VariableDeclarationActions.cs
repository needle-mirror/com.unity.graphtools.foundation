using System;
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

        public static void DefaultReducer(State previousState, CreateGraphVariableDeclarationAction action)
        {
            previousState.PushUndo(action);

            var graphModel = previousState.CurrentGraphModel;
            var variableDeclaration = graphModel.CreateGraphVariableDeclaration(action.VariableName, action.TypeHandle,
                action.ModifierFlags, action.IsExposed, null, action.Guid);
            previousState.EditorDataModel.ElementModelToRename = variableDeclaration;
            previousState.MarkForUpdate(UpdateFlags.RequestRebuild);
        }
    }

    public class ReorderGraphVariableDeclarationAction : BaseAction
    {
        public readonly IVariableDeclarationModel VariableDeclarationModel;
        public readonly int Index;

        public ReorderGraphVariableDeclarationAction()
        {
            UndoString = "Reorder Variable";
        }

        public ReorderGraphVariableDeclarationAction(IVariableDeclarationModel variableDeclarationModel, int index) : this()
        {
            VariableDeclarationModel = variableDeclarationModel;
            Index = index;
        }

        public static void DefaultReducer(State previousState, ReorderGraphVariableDeclarationAction action)
        {
            previousState.PushUndo(action);
            previousState.CurrentGraphModel.ReorderGraphVariableDeclaration(action.VariableDeclarationModel, action.Index);
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

        public static void DefaultReducer(State previousState, ChangeVariableTypeAction action)
        {
            var graphModel = previousState.CurrentGraphModel;

            if (action.Handle.IsValid)
            {
                previousState.PushUndo(action);

                if (action.VariableDeclarationModel.DataType != action.Handle)
                    action.VariableDeclarationModel.CreateInitializationValue();

                action.VariableDeclarationModel.DataType = action.Handle;

                foreach (var usage in graphModel.FindReferencesInGraph<IVariableNodeModel>(action.VariableDeclarationModel))
                    usage.UpdateTypeFromDeclaration();

                previousState.MarkForUpdate(UpdateFlags.RequestRebuild);
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

        public static void DefaultReducer(State previousState, UpdateExposedAction action)
        {
            previousState.PushUndo(action);

            action.VariableDeclarationModel.IsExposed = action.Exposed;

            previousState.MarkForUpdate(UpdateFlags.RequestRebuild);
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

        public static void DefaultReducer(State previousState, UpdateTooltipAction action)
        {
            previousState.PushUndo(action);
            action.VariableDeclarationModel.Tooltip = action.Tooltip;
        }
    }
}
