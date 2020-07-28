using System;
using System.Collections.Generic;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using System.Linq;
using Unity.Properties;
using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.SmartSearch;
using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.GraphViewModel;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting
{
    public class DisconnectNodeAction : IAction
    {
        public IGTFNodeModel[] NodeModels;

        public DisconnectNodeAction()
        {
        }

        public DisconnectNodeAction(params IGTFNodeModel[] nodeModels)
        {
            NodeModels = nodeModels;
        }
    }

    public class RemoveNodesAction : IAction
    {
        public readonly IGTFNodeModel[] ElementsToRemove;
        public readonly IInOutPortsNode[] NodesToBypass;

        public RemoveNodesAction(IInOutPortsNode[] nodesToBypass, IGTFNodeModel[] elementsToRemove)
        {
            ElementsToRemove = elementsToRemove;
            NodesToBypass = nodesToBypass;
        }
    }

    public class CreateNodeFromSearcherAction : IAction
    {
        public Vector2 Position;
        public GraphNodeModelSearcherItem SelectedItem;
        public GUID[] Guids;

        public CreateNodeFromSearcherAction()
        {
        }

        public CreateNodeFromSearcherAction(Vector2 position,
                                            GraphNodeModelSearcherItem selectedItem, IReadOnlyList<GUID> guids)
        {
            Position = position;
            SelectedItem = selectedItem;
            Guids = guids?.ToArray();
        }
    }

    public class SetNodeEnabledStateAction : IAction
    {
        public IGTFNodeModel[] NodeToConvert;
        public ModelState State;

        public SetNodeEnabledStateAction() {}

        public SetNodeEnabledStateAction(IGTFNodeModel[] nodeModel, ModelState state)
        {
            State = state;
            NodeToConvert = nodeModel;
        }
    }

    public class UpdateConstantNodeActionValue : IAction
    {
        public IConstant Constant;
        public IGTFConstantNodeModel NodeModel;
        public object Value;

        public UpdateConstantNodeActionValue()
        {
        }

        public UpdateConstantNodeActionValue(IConstant constant, object value, IGTFConstantNodeModel owner)
        {
            Constant = constant;
            Value = value;
            NodeModel = owner;
        }
    }

    public class UpdateModelPropertyValueAction : IAction
    {
        public IGTFGraphElementModel GraphElementModel;
        public object NewValue;
        public string Path;

        public UpdateModelPropertyValueAction()
        {
        }

        public UpdateModelPropertyValueAction(IGTFGraphElementModel graphElementModel, PropertyPath path, object newValue)
        {
            GraphElementModel = graphElementModel;
            Path = path.ToString();
            NewValue = newValue;
        }
    }
}
