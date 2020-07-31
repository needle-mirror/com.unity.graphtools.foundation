using System;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive.GraphElements;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.SmartSearch;
using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.GraphViewModel;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting
{
    static class NodeReducers
    {
        public static void Register(Store store)
        {
            store.RegisterReducer<State, DisconnectNodeAction>(DisconnectNode);
            store.RegisterReducer<State, CreateNodeFromSearcherAction>(CreateNodeFromSearcher);
            store.RegisterReducer<State, SetNodeEnabledStateAction>(SetNodeEnabledState);
            store.RegisterReducer<State, SetNodePositionAction>(SetPosition);
            store.RegisterReducer<State, SetNodeCollapsedAction>(SetCollapsed);
            store.RegisterReducer<State, UpdateConstantNodeActionValue>(UpdateConstantNodeValue);
        }

        static State CreateNodeFromSearcher(State previousState, CreateNodeFromSearcherAction action)
        {
            var nodes = action.SelectedItem.CreateElements.Invoke(
                new GraphNodeCreationData(previousState.CurrentGraphModel, action.Position, guids: action.Guids));

            if (nodes.Any(n => n is EdgeModel))
                previousState.CurrentGraphModel.LastChanges.ElementsToAutoAlign.AddRange(nodes);

            previousState.MarkForUpdate(UpdateFlags.GraphTopology);
            return previousState;
        }

        static State DisconnectNode(State previousState, DisconnectNodeAction action)
        {
            var graphModel = (GraphModel)previousState.CurrentGraphModel;

            foreach (IGTFNodeModel nodeModel in action.NodeModels)
            {
                var edgeModels = graphModel.GetEdgesConnections(nodeModel);

                graphModel.DeleteEdges(edgeModels);
            }

            return previousState;
        }

        static State SetNodeEnabledState(State previousState, SetNodeEnabledStateAction action)
        {
            Undo.RegisterCompleteObjectUndo((Object)previousState.AssetModel, action.State == ModelState.Enabled ? "Enable Nodes" : "Disable Nodes");
            EditorUtility.SetDirty((Object)previousState.AssetModel);
            foreach (var nodeModel in action.NodeToConvert.OfType<NodeModel>())
                nodeModel.State = action.State;
            previousState.MarkForUpdate(UpdateFlags.GraphTopology);
            return previousState;
        }

        static State SetPosition(State previousState, SetNodePositionAction action)
        {
            Undo.RegisterCompleteObjectUndo((Object)previousState.AssetModel, "Move");

            foreach (var model in action.Models)
            {
                if (model != null)
                {
                    model.Position = action.Value;
                }
                previousState.MarkForUpdate(UpdateFlags.UpdateView, model);
            }

            return previousState;
        }

        static State SetCollapsed(State previousState, SetNodeCollapsedAction action)
        {
            Undo.RegisterCompleteObjectUndo((Object)previousState.AssetModel, "Collapse Node");

            foreach (var model in action.Models)
            {
                if (model is ICollapsible nodeModel)
                {
                    nodeModel.Collapsed = action.Value;
                }
                previousState.MarkForUpdate(UpdateFlags.UpdateView, model);
            }

            return previousState;
        }

        static State UpdateConstantNodeValue(State previousState, UpdateConstantNodeActionValue actionValue)
        {
            Undo.RegisterCompleteObjectUndo((Object)previousState.AssetModel, "Update Node Value");

            if (actionValue.NodeModel == null || actionValue.NodeModel.OutputPort.IsConnected)
            {
                previousState.MarkForUpdate(UpdateFlags.RequestCompilation);
            }

            actionValue.Constant.ObjectValue = actionValue.Value;

            return previousState;
        }
    }
}
