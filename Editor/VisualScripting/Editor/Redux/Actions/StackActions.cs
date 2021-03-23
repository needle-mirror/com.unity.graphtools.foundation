using System;
using System.Collections.Generic;
using UnityEditor.EditorCommon.Redux;
using UnityEditor.VisualScripting.Editor.SmartSearch;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model;
using UnityEditor.VisualScripting.Model.Stencils;
using UnityEngine;

namespace UnityEditor.VisualScripting.Editor
{
    public struct StackCreationOptions
    {
        public Vector2 Position;
        public readonly string Guid;
        public List<INodeModel> NodeModels;

        public StackCreationOptions(Vector2 position, List<INodeModel> nodeModels = null, string guid = null) : this()
        {
            Position = position;
            Guid = guid;
            NodeModels = nodeModels ?? new List<INodeModel>();
        }
    }

    public class CreateStacksForNodesAction : IAction
    {
        public readonly List<StackCreationOptions> Stacks;

        public CreateStacksForNodesAction(List<StackCreationOptions> stacks)
        {
            Stacks = stacks;
        }
    }

    public class ChangeStackedNodeAction : IAction
    {
        public readonly INodeModel OldNodeModel;
        public readonly IStackModel StackModel;
        public readonly StackNodeModelSearcherItem SelectedItem;

        public ChangeStackedNodeAction(INodeModel oldNodeModel, IStackModel stackModel,
                                       StackNodeModelSearcherItem selectedItem)
        {
            OldNodeModel = oldNodeModel;
            StackModel = stackModel;
            SelectedItem = selectedItem;
        }
    }

    public class MoveStackedNodesAction : IAction
    {
        public readonly IReadOnlyCollection<INodeModel> NodeModels;
        public readonly IStackModel StackModel;
        public readonly int Index;

        public MoveStackedNodesAction(IReadOnlyCollection<INodeModel> nodeModels, IStackModel stackModel, int index)
        {
            NodeModels = nodeModels;
            StackModel = stackModel;
            Index = index;
        }
    }

    public class SplitStackAction : IAction
    {
        public readonly IStackModel StackModel;
        public readonly int SplitIndex;

        public SplitStackAction(IStackModel stackModel, int splitIndex)
        {
            StackModel = stackModel;
            SplitIndex = splitIndex;
        }
    }

    public class MergeStackAction : IAction
    {
        public readonly IStackModel StackModelA;
        public readonly IStackModel StackModelB;

        public MergeStackAction(IStackModel stackModelA, IStackModel stackModelB)
        {
            StackModelA = stackModelA;
            StackModelB = stackModelB;
        }
    }

    public class CreateStackedNodeFromSearcherAction : IAction
    {
        public readonly IStackModel StackModel;
        public readonly int Index;
        public readonly StackNodeModelSearcherItem SelectedItem;
        public readonly IReadOnlyList<GUID> Guids;

        public CreateStackedNodeFromSearcherAction(IStackModel stackModel, int index,
                                                   StackNodeModelSearcherItem selectedItem, IReadOnlyList<GUID> guids = null)
        {
            StackModel = stackModel;
            Index = index;
            SelectedItem = selectedItem;
            Guids = guids;
        }
    }

    public class UpdateFunctionReturnTypeAction : IAction
    {
        public readonly IFunctionModel FunctionModel;
        public readonly TypeHandle NewType;

        public UpdateFunctionReturnTypeAction(IFunctionModel functionModel, TypeHandle newType)
        {
            FunctionModel = functionModel;
            NewType = newType;
        }
    }
}
