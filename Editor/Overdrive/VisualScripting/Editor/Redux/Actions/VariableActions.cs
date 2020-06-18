using System;
using System.Collections.Generic;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.GraphViewModel;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting
{
    public class CreateVariableNodesAction : IAction
    {
        public readonly List<Tuple<IVariableDeclarationModel, Vector2>> VariablesToCreate;
        public readonly IPortModel ConnectAfterCreation;
        public readonly IEnumerable<IGTFEdgeModel> EdgeModelsToDelete;
        public readonly bool AutoAlign;

        public CreateVariableNodesAction(List<Tuple<IVariableDeclarationModel, Vector2>> variablesToCreate, bool autoAlign = false)
        {
            VariablesToCreate = variablesToCreate;
            AutoAlign = autoAlign;
        }

        public CreateVariableNodesAction(IVariableDeclarationModel graphElementModel, Vector2 mousePosition, IEnumerable<IGTFEdgeModel> edgeModelsToDelete = null, IPortModel connectAfterCreation = null, bool autoAlign = false)
        {
            VariablesToCreate = new List<Tuple<IVariableDeclarationModel, Vector2>>();
            VariablesToCreate.Add(Tuple.Create(graphElementModel, mousePosition));
            EdgeModelsToDelete = edgeModelsToDelete;
            ConnectAfterCreation = connectAfterCreation;
            AutoAlign = autoAlign;
        }
    }

    public class CreateConstantNodeAction : IAction
    {
        public readonly string Name;
        public readonly TypeHandle Type;
        public readonly Vector2 Position;
        public readonly GUID? Guid;

        public CreateConstantNodeAction(string name, TypeHandle type, Vector2 position, GUID? guid = null)
        {
            Name = name;
            Type = type;
            Position = position;
            Guid = guid;
        }
    }

    public class CreateSystemConstantNodeAction : IAction
    {
        public readonly string Name;
        public readonly TypeHandle ReturnType;
        public readonly TypeHandle DeclaringType;
        public readonly string Identifier;
        public readonly Vector2 Position;
        public readonly string Guid;

        public CreateSystemConstantNodeAction(string name, TypeHandle returnType, TypeHandle declaringType, string identifier, Vector2 position, string guid = null)
        {
            Name = name;
            ReturnType = returnType;
            DeclaringType = declaringType;
            Identifier = identifier;
            Position = position;
            Guid = guid;
        }
    }

    public class CreateGraphVariableDeclarationAction : IAction
    {
        public readonly string Name;
        public readonly bool IsExposed;
        public readonly TypeHandle TypeHandle;
        public readonly ModifierFlags ModifierFlags;

        public CreateGraphVariableDeclarationAction(string name, bool isExposed, TypeHandle typeHandle, ModifierFlags modifierFlags = ModifierFlags.None)
        {
            Name = name;
            IsExposed = isExposed;
            TypeHandle = typeHandle;
            ModifierFlags = modifierFlags;
        }
    }

    public class DuplicateGraphVariableDeclarationsAction : IAction
    {
        public readonly List<IVariableDeclarationModel> VariableDeclarationModels;

        public DuplicateGraphVariableDeclarationsAction(List<IVariableDeclarationModel> variableDeclarationModels)
        {
            VariableDeclarationModels = variableDeclarationModels;
        }
    }

    public class ReorderGraphVariableDeclarationAction : IAction
    {
        public readonly IVariableDeclarationModel VariableDeclarationModel;
        public readonly int Index;

        public ReorderGraphVariableDeclarationAction(IVariableDeclarationModel variableDeclarationModel, int index)
        {
            VariableDeclarationModel = variableDeclarationModel;
            Index = index;
        }
    }

    public class ConvertVariableNodesToConstantNodesAction : IAction
    {
        public readonly IVariableModel[] VariableModels;

        public ConvertVariableNodesToConstantNodesAction(params IVariableModel[] variableModels)
        {
            VariableModels = variableModels;
        }
    }

    public class ConvertConstantNodesToVariableNodesAction : IAction
    {
        public readonly IConstantNodeModel[] ConstantModels;

        public ConvertConstantNodesToVariableNodesAction(params IConstantNodeModel[] constantModels)
        {
            ConstantModels = constantModels;
        }
    }

    public class MoveVariableDeclarationAction : IAction
    {
        public readonly IVariableDeclarationModel VariableDeclarationModel;
        public readonly IHasVariableDeclaration Destination;

        public MoveVariableDeclarationAction(IVariableDeclarationModel variableDeclarationModel, IHasVariableDeclaration destination)
        {
            VariableDeclarationModel = variableDeclarationModel;
            Destination = destination;
        }
    }

    public class ReorderVariableDeclarationAction : IAction
    {
        public readonly IVariableDeclarationModel VariableDeclarationModel;
        public readonly int Index;

        public ReorderVariableDeclarationAction(IVariableDeclarationModel variableDeclarationModel, int index)
        {
            VariableDeclarationModel = variableDeclarationModel;
            Index = index;
        }
    }

    // Create a separate instance of the variable node for each connections on the original variable node.
    public class ItemizeVariableNodeAction : IAction
    {
        public readonly IVariableModel[] VariableModels;

        public ItemizeVariableNodeAction(params IVariableModel[] variableModels)
        {
            VariableModels = variableModels;
        }
    }

    // Create a separate instance of the constant node for each connections on the original constant node.
    public class ItemizeConstantNodeAction : IAction
    {
        public readonly IConstantNodeModel[] ConstantModels;

        public ItemizeConstantNodeAction(params IConstantNodeModel[] constantModels)
        {
            ConstantModels = constantModels;
        }
    }

    // Create a separate instance of the constant node for each connections on the original constant node.
    public class ItemizeSystemConstantNodeAction : IAction
    {
        public readonly ISystemConstantNodeModel[] ConstantModels;

        public ItemizeSystemConstantNodeAction(params ISystemConstantNodeModel[] constantModels)
        {
            ConstantModels = constantModels;
        }
    }

    public class ToggleLockConstantNodeAction : IAction
    {
        public readonly IConstantNodeModel[] ConstantNodeModels;

        public ToggleLockConstantNodeAction(params IConstantNodeModel[] constantNodeModels)
        {
            ConstantNodeModels = constantNodeModels;
        }
    }

    public class UpdateTypeAction : IAction
    {
        public readonly VariableDeclarationModel VariableDeclarationModel;
        public readonly TypeHandle Handle;

        public UpdateTypeAction(VariableDeclarationModel variableDeclarationModel, TypeHandle handle)
        {
            VariableDeclarationModel = variableDeclarationModel;
            Handle = handle;
        }
    }

    public class UpdateExposedAction : IAction
    {
        public readonly VariableDeclarationModel VariableDeclarationModel;
        public readonly bool Exposed;

        public UpdateExposedAction(VariableDeclarationModel variableDeclarationModel, bool exposed)
        {
            VariableDeclarationModel = variableDeclarationModel;
            Exposed = exposed;
        }
    }

    public class UpdateTooltipAction : IAction
    {
        public readonly VariableDeclarationModel VariableDeclarationModel;
        public readonly string Tooltip;

        public UpdateTooltipAction(VariableDeclarationModel variableDeclarationModel, string tooltip)
        {
            VariableDeclarationModel = variableDeclarationModel;
            Tooltip = tooltip;
        }
    }
}
