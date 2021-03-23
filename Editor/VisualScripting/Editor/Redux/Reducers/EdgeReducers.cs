using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEditor.Experimental.GraphView;
using UnityEditor.VisualScripting.Editor.SmartSearch;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model;
using UnityEditor.VisualScripting.Model.Stencils;
using UnityEngine;
using UnityEngine.Assertions;
using static UnityEditor.VisualScripting.Model.VSPreferences;
using Object = UnityEngine.Object;

namespace UnityEditor.VisualScripting.Editor
{
    public static class EdgeReducers
    {
        const int k_NodeOffset = 60;
        const int k_StackOffset = 120;

        public static void Register(Store store)
        {
            store.Register<CreateNodeFromLoopPortAction>(CreateNodeFromLoopPort);
            store.Register<CreateInsertLoopNodeAction>(CreateInsertLoopNode);
            store.Register<CreateNodeFromExecutionPortAction>(CreateNodeFromExecutionPort);
            store.Register<CreateNodeFromInputPortAction>(CreateGraphNodeFromInputPort);
            store.Register<CreateStackedNodeFromOutputPortAction>(CreateStackedNodeFromOutputPort);
            store.Register<CreateNodeFromOutputPortAction>(CreateNodeFromOutputPort);
            store.Register<CreateEdgeAction>(CreateEdge);
            store.Register<SplitEdgeAndInsertNodeAction>(SplitEdgeAndInsertNode);
            store.Register<CreateNodeOnEdgeAction>(CreateNodeOnEdge);
        }

        static State CreateNodeFromLoopPort(State previousState, CreateNodeFromLoopPortAction action)
        {
            var graphModel = (VSGraphModel)previousState.CurrentGraphModel;
            graphModel.DeleteEdges(action.EdgesToDelete);

            var stackPosition = action.Position - Vector2.right * k_StackOffset;

            if (action.PortModel.NodeModel is LoopNodeModel loopNodeModel)
            {
                var loopStackType = loopNodeModel.MatchingStackType;
                var loopStack = graphModel.CreateLoopStack(loopStackType, stackPosition);

                graphModel.CreateEdge(loopStack.InputPort, action.PortModel);
            }
            else
            {
                var stack = graphModel.CreateStack(null, stackPosition);
                graphModel.CreateEdge(stack.InputPorts[0], action.PortModel);
            }

            graphModel.LastChanges?.ChangedElements.Add(action.PortModel.NodeModel);

            return previousState;
        }

        static State CreateInsertLoopNode(State previousState, CreateInsertLoopNodeAction action)
        {
            var graphModel = (VSGraphModel)previousState.CurrentGraphModel;
            Undo.RegisterCompleteObjectUndo((Object)graphModel.AssetModel, "Create InsertLoop Node");
            Assert.IsTrue(graphModel.AssetModel as Object);
            graphModel.DeleteEdges(action.EdgesToDelete);

            var loopNode = ((StackBaseModel)action.StackModel).CreateStackedNode(
                action.LoopStackModel.MatchingStackedNodeType, "", action.Index);

            graphModel.CreateEdge(action.PortModel, loopNode.OutputsByDisplayOrder.First());
            graphModel.LastChanges?.ChangedElements.Add(action.PortModel.NodeModel);

            return previousState;
        }

        static State CreateNodeFromExecutionPort(State previousState, CreateNodeFromExecutionPortAction action)
        {
            var graphModel = (VSGraphModel)previousState.CurrentGraphModel;
            graphModel.DeleteEdges(action.EdgesToDelete);

            var stackPosition = action.Position - Vector2.right * k_StackOffset;
            var stack = graphModel.CreateStack(string.Empty, stackPosition);

            if (action.PortModel.Direction == Direction.Output)
                graphModel.CreateEdge(stack.InputPorts[0], action.PortModel);
            else
                graphModel.CreateEdge(action.PortModel, stack.OutputPorts[0]);

            graphModel.LastChanges?.ChangedElements.Add(action.PortModel.NodeModel);

            return previousState;
        }

        static State CreateGraphNodeFromInputPort(State previousState, CreateNodeFromInputPortAction action)
        {
            var graphModel = (VSGraphModel)previousState.CurrentGraphModel;
            graphModel.DeleteEdges(action.EdgesToDelete);

            var position = action.Position - Vector2.up * k_NodeOffset;
            var elementModels = action.SelectedItem.CreateElements.Invoke(
                new GraphNodeCreationData(graphModel, position));

            if (elementModels.Length == 0 || !(elementModels[0] is INodeModel selectedNodeModel))
                return previousState;

            var outputPortModel = action.PortModel.DataType == TypeHandle.Unknown
                ? selectedNodeModel.OutputsByDisplayOrder.FirstOrDefault()
                : GetFirstPortModelOfType(action.PortModel.DataType, selectedNodeModel.OutputsByDisplayOrder);

            if (outputPortModel != null)
            {
                var newEdge = graphModel.CreateEdge(action.PortModel, outputPortModel);
                if (newEdge != null && previousState.Preferences.GetBool(BoolPref.AutoAlignDraggedEdges))
                    graphModel.LastChanges?.ModelsToAutoAlign.Add(newEdge);
            }

            graphModel.LastChanges?.ChangedElements.Add(action.PortModel.NodeModel);

            return previousState;
        }

        static State CreateStackedNodeFromOutputPort(State previousState, CreateStackedNodeFromOutputPortAction action)
        {
            var graphModel = (VSGraphModel)previousState.CurrentGraphModel;
            Undo.RegisterCompleteObjectUndo((Object)graphModel.AssetModel, "Create Node From Output Port");
            graphModel.DeleteEdges(action.EdgesToDelete);

            var elementModels = action.SelectedItem.CreateElements.Invoke(
                new StackNodeCreationData(action.StackModel, action.Index));

            if (elementModels.Length == 0 || !(elementModels[0] is INodeModel selectedNodeModel))
                return previousState;

            var outputPortModel = action.PortModel;
            var newInput = selectedNodeModel.InputsByDisplayOrder.FirstOrDefault();
            if (newInput != null)
            {
                CreateItemizedNode(previousState, graphModel, ref outputPortModel);
                var newEdge = graphModel.CreateEdge(newInput, outputPortModel);
                if (newEdge != null && previousState.Preferences.GetBool(BoolPref.AutoAlignDraggedEdges))
                    graphModel.LastChanges?.ModelsToAutoAlign.Add(newEdge);
            }

            graphModel.LastChanges?.ChangedElements.Add(action.PortModel.NodeModel);

            return previousState;
        }

        static State CreateNodeFromOutputPort(State previousState, CreateNodeFromOutputPortAction action)
        {
            var graphModel = (VSGraphModel)previousState.CurrentGraphModel;
            graphModel.DeleteEdges(action.EdgesToDelete);

            var position = action.Position - Vector2.up * k_NodeOffset;
            var elementModels = action.SelectedItem.CreateElements.Invoke(
                new GraphNodeCreationData(graphModel, position));

            if (!elementModels.Any() || !(elementModels[0] is INodeModel selectedNodeModel))
                return previousState;

            var inputPortModel = action.PortModel.DataType == TypeHandle.Unknown
                ? selectedNodeModel.InputsByDisplayOrder.FirstOrDefault()
                : GetFirstPortModelOfType(action.PortModel.DataType, selectedNodeModel.InputsByDisplayOrder);

            if (inputPortModel == null)
                return previousState;

            var outputPortModel = action.PortModel;

            CreateItemizedNode(previousState, graphModel, ref outputPortModel);
            var newEdge = graphModel.CreateEdge(inputPortModel, outputPortModel);

            if (newEdge != null && previousState.Preferences.GetBool(BoolPref.AutoAlignDraggedEdges))
                graphModel.LastChanges?.ModelsToAutoAlign.Add(newEdge);

            graphModel.LastChanges?.ChangedElements.Add(action.PortModel.NodeModel);

            return previousState;
        }

        static State CreateNodeOnEdge(State previousState, CreateNodeOnEdgeAction action)
        {
            var edgeInput = action.EdgeModel.InputPortModel;
            var edgeOutput = action.EdgeModel.OutputPortModel;

            // Instantiate node
            var graphModel = (VSGraphModel)previousState.CurrentGraphModel;

            var position = action.Position - Vector2.up * k_NodeOffset;

            List<GUID> guids = action.Guid.HasValue ? new List<GUID> { action.Guid.Value } : null;

            var elementModels = action.SelectedItem.CreateElements.Invoke(
                new GraphNodeCreationData(graphModel, position, guids: guids));

            if (elementModels.Length == 0 || !(elementModels[0] is INodeModel selectedNodeModel))
                return previousState;

            // Delete old edge
            graphModel.DeleteEdge(action.EdgeModel);

            // Connect input port
            var inputPortModel = selectedNodeModel is FunctionCallNodeModel
                ? selectedNodeModel.InputsByDisplayOrder.FirstOrDefault(p =>
                p.DataType.Equals(edgeOutput.DataType))
                : selectedNodeModel.InputsByDisplayOrder.FirstOrDefault();

            if (inputPortModel != null)
                graphModel.CreateEdge(inputPortModel, edgeOutput);

            // Find first matching output type and connect it
            var outputPortModel = GetFirstPortModelOfType(edgeInput.DataType,
                selectedNodeModel.OutputsByDisplayOrder);

            if (outputPortModel != null)
                graphModel.CreateEdge(edgeInput, outputPortModel);

            return previousState;
        }

        public static State CreateEdge(State previousState, CreateEdgeAction action)
        {
            var graphModel = (VSGraphModel)previousState.CurrentGraphModel;

            if (action.EdgeModelsToDelete != null)
                graphModel.DeleteEdges(action.EdgeModelsToDelete);

            IPortModel outputPortModel = action.OutputPortModel;
            IPortModel inputPortModel = action.InputPortModel;

            if (inputPortModel.NodeModel is LoopStackModel loopStackModel)
            {
                if (!loopStackModel.MatchingStackedNodeType.IsInstanceOfType(outputPortModel.NodeModel))
                    return previousState;
            }

            CreateItemizedNode(previousState, graphModel, ref outputPortModel);
            graphModel.CreateEdge(inputPortModel, outputPortModel);

            if (action.PortAlignment.HasFlag(CreateEdgeAction.PortAlignmentType.Input))
                graphModel.LastChanges.ModelsToAutoAlign.Add(inputPortModel.NodeModel);
            if (action.PortAlignment.HasFlag(CreateEdgeAction.PortAlignmentType.Output))
                graphModel.LastChanges.ModelsToAutoAlign.Add(outputPortModel.NodeModel);

            return previousState;
        }

        static State SplitEdgeAndInsertNode(State previousState, SplitEdgeAndInsertNodeAction action)
        {
            Assert.IsTrue(action.NodeModel.InputsById.Count > 0);
            Assert.IsTrue(action.NodeModel.OutputsById.Count > 0);

            var graphModel = ((VSGraphModel)previousState.CurrentGraphModel);
            var edgeInput = action.EdgeModel.InputPortModel;
            var edgeOutput = action.EdgeModel.OutputPortModel;
            graphModel.DeleteEdge(action.EdgeModel);
            graphModel.CreateEdge(edgeInput, action.NodeModel.OutputsByDisplayOrder.First());
            graphModel.CreateEdge(action.NodeModel.InputsByDisplayOrder.First(), edgeOutput);

            return previousState;
        }

        [CanBeNull]
        static IPortModel GetFirstPortModelOfType(TypeHandle typeHandle, IEnumerable<IPortModel> portModels)
        {
            Stencil stencil = portModels.First().GraphModel.Stencil;
            IPortModel unknownPortModel = null;

            // Return the first matching Input portModel
            // If no match was found, return the first Unknown typed portModel
            // Else return null.
            foreach (IPortModel portModel in portModels)
            {
                if (portModel.DataType == TypeHandle.Unknown && unknownPortModel == null)
                {
                    unknownPortModel = portModel;
                }

                if (typeHandle.IsAssignableFrom(portModel.DataType, stencil))
                {
                    return portModel;
                }
            }

            return unknownPortModel;
        }

        static void CreateItemizedNode(State state, VSGraphModel graphModel, ref IPortModel outputPortModel)
        {
            ItemizeOptions currentItemizeOptions = state.Preferences.CurrentItemizeOptions;

            // automatically itemize, i.e. duplicate variables as they get connected
            if (outputPortModel.Connected && currentItemizeOptions != ItemizeOptions.Nothing)
            {
                var nodeToConnect = outputPortModel.NodeModel;
                var offset = Vector2.up * k_NodeOffset;

                if (currentItemizeOptions.HasFlag(ItemizeOptions.Constants)
                    && nodeToConnect is ConstantNodeModel constantModel)
                {
                    string newName = string.IsNullOrEmpty(constantModel.Title)
                        ? "Temporary"
                        : constantModel.Title + "Copy";
                    nodeToConnect = graphModel.CreateConstantNode(
                        newName,
                        constantModel.Type.GenerateTypeHandle(graphModel.Stencil),
                        constantModel.Position + offset
                    );
                    ((ConstantNodeModel)nodeToConnect).ObjectValue = constantModel.ObjectValue;
                }
                else if (currentItemizeOptions.HasFlag(ItemizeOptions.Variables)
                         && nodeToConnect is VariableNodeModel variableModel)
                {
                    nodeToConnect = graphModel.CreateVariableNode(variableModel.DeclarationModel,
                        variableModel.Position + offset);
                }
                else if (currentItemizeOptions.HasFlag(ItemizeOptions.Variables)
                         && nodeToConnect is ThisNodeModel thisModel)
                {
                    nodeToConnect = graphModel.CreateNode<ThisNodeModel>("this", thisModel.Position + offset);
                }
                else if (currentItemizeOptions.HasFlag(ItemizeOptions.SystemConstants) &&
                         nodeToConnect is SystemConstantNodeModel sysConstModel)
                {
                    Action<SystemConstantNodeModel> preDefineSetup = m =>
                    {
                        m.ReturnType = sysConstModel.ReturnType;
                        m.DeclaringType = sysConstModel.DeclaringType;
                        m.Identifier = sysConstModel.Identifier;
                    };
                    nodeToConnect = graphModel.CreateNode(sysConstModel.Title, sysConstModel.Position + offset, SpawnFlags.Default, preDefineSetup);
                }

                outputPortModel = nodeToConnect.OutputsById[outputPortModel.UniqueId];
            }
        }
    }
}
