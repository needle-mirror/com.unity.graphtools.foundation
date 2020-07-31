using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.GraphViewModel;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting
{
    public class CreateVariableNodesAction : IAction
    {
        public List<(IGTFVariableDeclarationModel, SerializableGUID, Vector2)> VariablesToCreate;
        public IGTFPortModel ConnectAfterCreation;
        public IGTFEdgeModel[] EdgeModelsToDelete;
        public bool AutoAlign;

        public CreateVariableNodesAction()
        {
        }

        public CreateVariableNodesAction(List<(IGTFVariableDeclarationModel, SerializableGUID, Vector2)> variablesToCreate, bool autoAlign = false)
        {
            VariablesToCreate = variablesToCreate;
            AutoAlign = autoAlign;
        }

        public CreateVariableNodesAction(IGTFVariableDeclarationModel graphElementModel, Vector2 mousePosition, IEnumerable<IGTFEdgeModel> edgeModelsToDelete = null, IGTFPortModel connectAfterCreation = null, bool autoAlign = false)
        {
            VariablesToCreate = new List<(IGTFVariableDeclarationModel, SerializableGUID, Vector2)>();
            VariablesToCreate.Add((graphElementModel, GUID.Generate(), mousePosition));
            EdgeModelsToDelete = edgeModelsToDelete?.ToArray();
            ConnectAfterCreation = connectAfterCreation;
            AutoAlign = autoAlign;
        }
    }

    public class CreateConstantNodeAction : IAction
    {
        // [CreateProperty]public string Name { get; private set; }
        public string Name;
        public TypeHandle Type;
        public Vector2 Position;
        public GUID? Guid;

        public CreateConstantNodeAction()
        {
        }

        public CreateConstantNodeAction(string name, TypeHandle type, Vector2 position, GUID? guid = null)
        {
            Name = name;
            Type = type;
            Position = position;
            Guid = guid;
        }
    }

    public class CreateGraphVariableDeclarationAction : IAction
    {
        public string Name;
        public bool IsExposed;
        public TypeHandle TypeHandle;
        public GUID Guid;
        public ModifierFlags ModifierFlags;

        public CreateGraphVariableDeclarationAction()
        {
        }

        public CreateGraphVariableDeclarationAction(string name, bool isExposed, TypeHandle typeHandle, ModifierFlags modifierFlags = ModifierFlags.None, GUID? guid = null)
        {
            Name = name;
            IsExposed = isExposed;
            TypeHandle = typeHandle;
            Guid = guid ?? GUID.Generate();
            ModifierFlags = modifierFlags;
        }
    }

    public class DuplicateGraphVariableDeclarationsAction : IAction
    {
        public List<IGTFVariableDeclarationModel> VariableDeclarationModels;

        public DuplicateGraphVariableDeclarationsAction()
        {
        }

        public DuplicateGraphVariableDeclarationsAction(List<IGTFVariableDeclarationModel> variableDeclarationModels)
        {
            VariableDeclarationModels = variableDeclarationModels;
        }
    }

    public class ReorderGraphVariableDeclarationAction : IAction
    {
        public readonly IGTFVariableDeclarationModel VariableDeclarationModel;
        public readonly int Index;

        public ReorderGraphVariableDeclarationAction(IGTFVariableDeclarationModel variableDeclarationModel, int index)
        {
            VariableDeclarationModel = variableDeclarationModel;
            Index = index;
        }
    }

    public class ConvertVariableNodesToConstantNodesAction : IAction
    {
        public readonly IGTFVariableNodeModel[] VariableModels;

        public ConvertVariableNodesToConstantNodesAction(params IGTFVariableNodeModel[] variableModels)
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
        public readonly IGTFVariableNodeModel[] VariableModels;

        public ItemizeVariableNodeAction(params IGTFVariableNodeModel[] variableModels)
        {
            VariableModels = variableModels;
        }
    }

    // Create a separate instance of the constant node for each connections on the original constant node.
    public class ItemizeConstantNodeAction : IAction
    {
        public readonly IGTFConstantNodeModel[] ConstantModels;

        public ItemizeConstantNodeAction(params IGTFConstantNodeModel[] constantModels)
        {
            ConstantModels = constantModels;
        }
    }

    public class ToggleLockConstantNodeAction : IAction
    {
        public readonly IGTFConstantNodeModel[] ConstantNodeModels;

        public ToggleLockConstantNodeAction(params IGTFConstantNodeModel[] constantNodeModels)
        {
            ConstantNodeModels = constantNodeModels;
        }
    }

    public class UpdateTypeAction : IAction
    {
        public IGTFVariableDeclarationModel VariableDeclarationModel;
        public TypeHandle Handle;

        public UpdateTypeAction()
        {
        }

        public UpdateTypeAction(IGTFVariableDeclarationModel variableDeclarationModel, TypeHandle handle)
        {
            VariableDeclarationModel = variableDeclarationModel;
            Handle = handle;
        }
    }

    public class UpdateExposedAction : IAction
    {
        public IGTFVariableDeclarationModel VariableDeclarationModel;
        public bool Exposed;

        public UpdateExposedAction()
        {
        }

        public UpdateExposedAction(IGTFVariableDeclarationModel variableDeclarationModel, bool exposed)
        {
            VariableDeclarationModel = variableDeclarationModel;
            Exposed = exposed;
        }
    }

    public class UpdateTooltipAction : IAction
    {
        public IGTFVariableDeclarationModel VariableDeclarationModel;
        public string Tooltip;

        public UpdateTooltipAction()
        {
        }

        public UpdateTooltipAction(IGTFVariableDeclarationModel variableDeclarationModel, string tooltip)
        {
            VariableDeclarationModel = variableDeclarationModel;
            Tooltip = tooltip;
        }
    }
}
