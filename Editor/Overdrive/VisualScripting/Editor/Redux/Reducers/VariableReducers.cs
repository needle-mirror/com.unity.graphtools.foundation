using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Properties;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.GraphViewModel;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting
{
    static class VariableReducers
    {
        public static void Register(Store store)
        {
            store.RegisterReducer<State, CreateVariableNodesAction>(CreateVariableNodes);
            store.RegisterReducer<State, CreateGraphVariableDeclarationAction>(CreateGraphVariableDeclaration);
            store.RegisterReducer<State, DuplicateGraphVariableDeclarationsAction>(DuplicateGraphVariableDeclarations);
            store.RegisterReducer<State, CreateConstantNodeAction>(CreateConstantNode);
            store.RegisterReducer<State, UpdateModelPropertyValueAction>(ChangeProperty);
            store.RegisterReducer<State, ReorderGraphVariableDeclarationAction>(ReorderGraphVariableDeclaration);
            store.RegisterReducer<State, ConvertVariableNodesToConstantNodesAction>(ConvertVariableNodesToConstantNodes);
            store.RegisterReducer<State, ConvertConstantNodesToVariableNodesAction>(ConvertConstantNodesToVariableNodes);
            store.RegisterReducer<State, ReorderVariableDeclarationAction>(ReorderVariableDeclaration);
            store.RegisterReducer<State, ItemizeVariableNodeAction>(ItemizeVariableNode);
            store.RegisterReducer<State, ItemizeConstantNodeAction>(ItemizeConstantNode);
            store.RegisterReducer<State, ToggleLockConstantNodeAction>(ToggleLockConstantNode);

            store.RegisterReducer<State, UpdateTypeAction>(UpdateType);
            store.RegisterReducer<State, UpdateExposedAction>(UpdateExposed);
            store.RegisterReducer<State, UpdateTooltipAction>(UpdateTooltip);
        }

        static State CreateVariableNodes(State previousState, CreateVariableNodesAction action)
        {
            if (action.VariablesToCreate.Count > 0)
            {
                if (action.ConnectAfterCreation != null)
                {
                    // Delete previous connections
                    if (action.EdgeModelsToDelete.Any())
                    {
                        previousState.CurrentGraphModel.DeleteEdges(action.EdgeModelsToDelete);
                    }
                }

                foreach ((IGTFVariableDeclarationModel, SerializableGUID, Vector2)tuple in action.VariablesToCreate)
                {
                    var vsGraphModel = previousState.CurrentGraphModel;

                    IGTFVariableNodeModel newVariable = vsGraphModel.CreateVariableNode(tuple.Item1, tuple.Item3, guid: tuple.Item2);

                    if (action.ConnectAfterCreation != null)
                    {
                        var newEdge = previousState.CurrentGraphModel.CreateEdge(action.ConnectAfterCreation, newVariable.OutputPort);
                        if (action.AutoAlign)
                        {
                            vsGraphModel.LastChanges.ElementsToAutoAlign.Add(newEdge);
                        }
                    }
                }
            }

            return previousState;
        }

        static State DuplicateGraphVariableDeclarations(State previousState, DuplicateGraphVariableDeclarationsAction action)
        {
            Undo.RegisterCompleteObjectUndo((Object)previousState.AssetModel, "Create Graph Variables");

            List<IGTFVariableDeclarationModel> duplicatedModels = previousState.CurrentGraphModel.DuplicateGraphVariableDeclarations(action.VariableDeclarationModels);
            previousState.EditorDataModel?.SelectElementsUponCreation(duplicatedModels, true);
            return previousState;
        }

        static State CreateConstantNode(State previousState, CreateConstantNodeAction action)
        {
            previousState.CurrentGraphModel.CreateConstantNode(action.Name, action.Type, action.Position);
            return previousState;
        }

        static State ChangeProperty(State previousState, UpdateModelPropertyValueAction valueAction)
        {
            Undo.RecordObject(previousState.AssetModel as Object, "Change constant value");

            if (valueAction.GraphElementModel is IPropertyVisitorNodeTarget target)
            {
                var targetTarget = target.Target;
                PropertyContainer.SetValue(ref targetTarget,  new PropertyPath(valueAction.Path), valueAction.NewValue);
                target.Target = targetTarget;
            }
            else
                PropertyContainer.SetValue(ref valueAction.GraphElementModel,  new PropertyPath(valueAction.Path), valueAction.NewValue);
            previousState.MarkForUpdate(UpdateFlags.RequestCompilation, valueAction.GraphElementModel);
            return previousState;
        }

        static State CreateGraphVariableDeclaration(State previousState, CreateGraphVariableDeclarationAction action)
        {
            var graphModel = previousState.CurrentGraphModel;
            var variableDeclaration = graphModel.CreateGraphVariableDeclaration(action.Name, action.TypeHandle, action.ModifierFlags, action.IsExposed, null, action.Guid);
            previousState.EditorDataModel.ElementModelToRename = variableDeclaration;
            previousState.MarkForUpdate(UpdateFlags.RequestRebuild);
            return previousState;
        }

        static State ReorderGraphVariableDeclaration(State previousState, ReorderGraphVariableDeclarationAction action)
        {
            Undo.RegisterCompleteObjectUndo((Object)previousState.AssetModel, "Reorder Graph Variable Declaration");
            previousState.CurrentGraphModel.ReorderGraphVariableDeclaration(action.VariableDeclarationModel, action.Index);
            return previousState;
        }

        static State ConvertVariableNodesToConstantNodes(State previousState, ConvertVariableNodesToConstantNodesAction action)
        {
            var graphModel = previousState.CurrentGraphModel;
            int variableModelsCount = action.VariableModels.Length;
            previousState.EditorDataModel.ElementModelToRename = null;
            foreach (var iVariableModel in action.VariableModels)
            {
                var variableModel = (VariableNodeModel)iVariableModel;
                if (graphModel.Stencil.GetConstantNodeValueType(variableModel.DataType) == null)
                    continue;
                var constantNode = (ConstantNodeModel)graphModel.CreateConstantNode(variableModel.Title, variableModel.DataType, variableModel.Position);
                // Rename converted item only if there are no other items to be converted
                if (variableModelsCount == 1)
                    previousState.EditorDataModel.ElementModelToRename = constantNode;
                constantNode.ObjectValue = variableModel.VariableDeclarationModel?.InitializationModel?.ObjectValue;

                foreach (var edge in graphModel.GetEdgesConnections(variableModel.OutputPort).ToList())
                {
                    graphModel.CreateEdge(edge.ToPort, constantNode.OutputPort);
                    graphModel.DeleteEdge(edge);
                }

                graphModel.DeleteNode(variableModel, DeleteConnections.False);
            }
            return previousState;
        }

        static State ConvertConstantNodesToVariableNodes(State previousState, ConvertConstantNodesToVariableNodesAction action)
        {
            var graphModel = previousState.CurrentGraphModel;
            foreach (var iConstantModel in action.ConstantModels)
            {
                var constantModel = (ConstantNodeModel)iConstantModel;

                var declarationModel = graphModel.CreateGraphVariableDeclaration(StringExtensions.CodifyString(constantModel.Type.FriendlyName()), constantModel.Type.GenerateTypeHandle(), ModifierFlags.None, true, constantModel.Value.CloneConstant());
                if (graphModel.CreateVariableNode(declarationModel, constantModel.Position) is IGTFVariableNodeModel variableModel)
                {
                    foreach (var edge in graphModel.GetEdgesConnections(constantModel.OutputPort).ToList())
                    {
                        graphModel.CreateEdge(edge.ToPort, variableModel.OutputPort);
                        graphModel.DeleteEdge(edge);
                    }
                }

                graphModel.DeleteNode(constantModel, DeleteConnections.False);
            }

            return previousState;
        }

        static State ReorderVariableDeclaration(State previousState, ReorderVariableDeclarationAction action)
        {
            if (action.VariableDeclarationModel is VariableDeclarationModel variableDeclarationModel)
            {
                var currentGraphModel = previousState.CurrentGraphModel;
                var currentIndex = currentGraphModel.VariableDeclarations.IndexOf(variableDeclarationModel);
                var insertionIndex = action.Index;
                if (currentIndex < insertionIndex)
                    insertionIndex--;

                currentGraphModel.VariableDeclarations.Remove(variableDeclarationModel);
                currentGraphModel.VariableDeclarations.Insert(insertionIndex, variableDeclarationModel);
            }
            return previousState;
        }

        static State ItemizeVariableNode(State previousState, ItemizeVariableNodeAction action)
        {
            var graphModel = previousState.CurrentGraphModel;
            foreach (var iVariableModel in action.VariableModels)
            {
                var variableModel = (VariableNodeModel)iVariableModel;
                var edges = graphModel.GetEdgesConnections(variableModel.OutputPort).ToList();

                for (var i = 1; i < edges.Count; i++)
                {
                    var edge = edges[i];
                    var newModel = graphModel.CreateVariableNode(variableModel.VariableDeclarationModel, variableModel.Position + i * 50 * Vector2.up);
                    graphModel.CreateEdge(edge.ToPort, newModel.OutputPort);
                    graphModel.DeleteEdge(edge);
                }
            }

            return previousState;
        }

        static State ItemizeConstantNode(State previousState, ItemizeConstantNodeAction action)
        {
            var graphModel = previousState.CurrentGraphModel;
            foreach (var iConstantModel in action.ConstantModels)
            {
                var constantModel = (ConstantNodeModel)iConstantModel;
                var edges = graphModel.GetEdgesConnections(constantModel.OutputPort).ToList();

                for (var i = 1; i < edges.Count; i++)
                {
                    var edge = edges[i];
                    var newName = (string.IsNullOrEmpty(constantModel.Title) ? "Temporary" : constantModel.Title) + i;
                    var newModel = (ConstantNodeModel)graphModel.CreateConstantNode(newName, constantModel.Type.GenerateTypeHandle(), constantModel.Position + i * 50 * Vector2.up);
                    newModel.ObjectValue = constantModel.ObjectValue;
                    graphModel.CreateEdge(edge.ToPort, newModel.OutputPort);
                    graphModel.DeleteEdge(edge);
                }
            }

            return previousState;
        }

        static State ToggleLockConstantNode(State previousState, ToggleLockConstantNodeAction action)
        {
            bool needUpdate = false;
            foreach (var constantNodeModel in action.ConstantNodeModels)
            {
                if (constantNodeModel is ConstantNodeModel model)
                {
                    Undo.RegisterCompleteObjectUndo(model.AssetModel as ScriptableObject, "Set IsLocked");
                    model.IsLocked = !model.IsLocked;
                    needUpdate = true;
                }
            }

            if (needUpdate)
            {
                IEnumerable<IGTFGraphElementModel> changedModels = action.ConstantNodeModels;
                previousState.CurrentGraphModel.LastChanges.ChangedElements.AddRange(changedModels);
            }

            return previousState;
        }

        static State UpdateType(State previousState, UpdateTypeAction action)
        {
            var graphModel = (GraphModel)previousState.CurrentGraphModel;

            if (action.Handle.IsValid)
            {
                Undo.RegisterCompleteObjectUndo(previousState.CurrentGraphModel.AssetModel as GraphAssetModel, "Update Type");

                if (action.VariableDeclarationModel.DataType != action.Handle)
                    action.VariableDeclarationModel.CreateInitializationValue();

                action.VariableDeclarationModel.DataType = action.Handle;

                foreach (var usage in graphModel.FindReferencesInGraph<VariableNodeModel>(action.VariableDeclarationModel))
                    usage.UpdateTypeFromDeclaration();

                previousState.MarkForUpdate(UpdateFlags.RequestRebuild);
            }

            return previousState;
        }

        static State UpdateExposed(State previousState, UpdateExposedAction action)
        {
            Undo.RegisterCompleteObjectUndo(previousState.CurrentGraphModel.AssetModel as GraphAssetModel, "Update Exposed");
            action.VariableDeclarationModel.IsExposed = action.Exposed;

            previousState.MarkForUpdate(UpdateFlags.RequestRebuild);

            return previousState;
        }

        static State UpdateTooltip(State previousState, UpdateTooltipAction action)
        {
            Undo.RegisterCompleteObjectUndo(previousState.CurrentGraphModel.AssetModel as GraphAssetModel, "Update Tooltip");
            action.VariableDeclarationModel.Tooltip = action.Tooltip;

            return previousState;
        }
    }
}
