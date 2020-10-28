using System.Collections.Generic;
using System.Linq;
using Unity.Properties;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public class SetNodeCollapsedAction : ModelAction<INodeModel, bool>
    {
        const string k_CollapseUndoStringSingular = "Collapse Node";
        const string k_CollapseUndoStringPlural = "Collapse Nodes";
        const string k_ExpandUndoStringSingular = "Expand Node";
        const string k_ExpandUndoStringPlural = "Expand Nodes";

        public SetNodeCollapsedAction()
            : base("Collapse Or Expand Node") {}

        public SetNodeCollapsedAction(IReadOnlyList<INodeModel> models, bool value)
            : base(value ? k_CollapseUndoStringSingular : k_ExpandUndoStringSingular,
                   value ? k_CollapseUndoStringPlural : k_ExpandUndoStringPlural,
                   models, value) {}

        public static void DefaultReducer(State previousState, SetNodeCollapsedAction action)
        {
            if (!action.Models.Any())
                return;

            previousState.PushUndo(action);

            foreach (var model in action.Models.OfType<ICollapsible>())
            {
                model.Collapsed = action.Value;
                previousState.MarkForUpdate(UpdateFlags.UpdateView, model as IGraphElementModel);
            }
        }
    }

    public class RenameElementAction : BaseAction
    {
        public IRenamable RenamableModel;
        public string ElementName;

        public RenameElementAction()
        {
            UndoString = "Rename Element";
        }

        public RenameElementAction(IRenamable renamableModel, string name) : this()
        {
            RenamableModel = renamableModel;
            ElementName = name;
        }

        public static void DefaultReducer(State previousState, RenameElementAction action)
        {
            previousState.PushUndo(action);

            action.RenamableModel.Rename(action.ElementName);

            GraphChangeList graphChangeList = previousState.CurrentGraphModel.LastChanges;

            var graphModel = previousState.CurrentGraphModel;

            if (action.RenamableModel is IVariableDeclarationModel variableDeclarationModel)
            {
                graphChangeList.BlackBoardChanged = true;

                // update usage names
                graphChangeList.ChangedElements.AddRange(graphModel.FindReferencesInGraph<IVariableNodeModel>(variableDeclarationModel));
            }
            else if (action.RenamableModel is IVariableNodeModel variableModel)
            {
                graphChangeList.BlackBoardChanged = true;

                variableDeclarationModel = variableModel.VariableDeclarationModel;
                graphChangeList.ChangedElements.Add(variableModel.VariableDeclarationModel);

                graphChangeList.ChangedElements.AddRange(graphModel.FindReferencesInGraph<IVariableNodeModel>(variableDeclarationModel));
            }
            else if (action.RenamableModel is IEdgePortalModel edgePortalModel)
            {
                var declarationModel = edgePortalModel.DeclarationModel as IGraphElementModel;
                graphChangeList.ChangedElements.Add(declarationModel);
                graphChangeList.ChangedElements.AddRange(graphModel.FindReferencesInGraph<IEdgePortalModel>(edgePortalModel.DeclarationModel));
            }
            else
                graphChangeList.ChangedElements.Add(action.RenamableModel as IGraphElementModel);

            previousState.MarkForUpdate(UpdateFlags.RequestCompilation | UpdateFlags.RequestRebuild);
        }
    }

    public class UpdateConstantNodeValueAction : BaseAction
    {
        public IConstant Constant;
        public IConstantNodeModel NodeModel;
        public object Value;

        public UpdateConstantNodeValueAction()
        {
            UndoString = "Update Node Value";
        }

        public UpdateConstantNodeValueAction(IConstant constant, object value, IConstantNodeModel owner) : this()
        {
            Constant = constant;
            Value = value;
            NodeModel = owner;
        }

        public static void DefaultReducer(State previousState, UpdateConstantNodeValueAction action)
        {
            previousState.PushUndo(action);

            if (action.NodeModel == null || action.NodeModel.OutputPort.IsConnected())
            {
                previousState.MarkForUpdate(UpdateFlags.RequestCompilation);
            }

            action.Constant.ObjectValue = action.Value;
        }
    }

    public class UpdatePortConstantAction : BaseAction
    {
        public IPortModel PortModel;
        public object NewValue;

        public UpdatePortConstantAction()
        {
            UndoString = "Update Port Value";
        }

        public UpdatePortConstantAction(IPortModel portModel, object newValue) : this()
        {
            PortModel = portModel;
            NewValue = newValue;
        }

        public static void DefaultReducer(State previousState, UpdatePortConstantAction action)
        {
            previousState.PushUndo(action);

            previousState.MarkForUpdate(UpdateFlags.RequestCompilation);

            if (action.PortModel.EmbeddedValue is IStringWrapperConstantModel stringWrapperConstantModel)
                stringWrapperConstantModel.StringValue = (string)action.NewValue;
            else
                action.PortModel.EmbeddedValue.ObjectValue = action.NewValue;
        }
    }

    public class DisconnectNodeAction : ModelAction<INodeModel>
    {
        const string k_UndoStringSingular = "Disconnect Node";
        const string k_UndoStringPlural = "Disconnect Nodes";

        public DisconnectNodeAction()
            : base(k_UndoStringSingular) {}

        public DisconnectNodeAction(params INodeModel[] nodeModels)
            : base(k_UndoStringSingular, k_UndoStringPlural, nodeModels) {}

        public static void DefaultReducer(State previousState, DisconnectNodeAction action)
        {
            if (!action.Models.Any())
                return;

            previousState.PushUndo(action);

            var graphModel = previousState.CurrentGraphModel;

            foreach (INodeModel nodeModel in action.Models)
            {
                var edgeModels = graphModel.GetEdgesConnections(nodeModel);

                graphModel.DeleteEdges(edgeModels);
            }
        }
    }

    public class BypassNodesAction : ModelAction<INodeModel>
    {
        const string k_UndoStringSingular = "Delete Element";
        const string k_UndoStringPlural = "Delete Elements";

        public readonly IInOutPortsNode[] NodesToBypass;

        public BypassNodesAction()
            : base(k_UndoStringSingular) {}

        public BypassNodesAction(IInOutPortsNode[] nodesToBypass, INodeModel[] elementsToRemove)
            : base(k_UndoStringSingular, k_UndoStringPlural, elementsToRemove)
        {
            NodesToBypass = nodesToBypass;
        }

        public static void DefaultReducer(State previousState, BypassNodesAction action)
        {
            previousState.PushUndo(action);

            var graphModel = previousState.CurrentGraphModel;
            BypassNodes(graphModel, action.NodesToBypass);
            graphModel.DeleteNodes(action.Models, DeleteConnections.False);
        }

        static void BypassNodes(IGraphModel graphModel, IInOutPortsNode[] actionNodeModels)
        {
            foreach (var model in actionNodeModels)
            {
                var inputEdgeModels = new List<IEdgeModel>();
                foreach (var portModel in model.InputsByDisplayOrder)
                {
                    inputEdgeModels.AddRange(graphModel.GetEdgesConnections(portModel));
                }

                if (!inputEdgeModels.Any())
                    continue;

                var outputEdgeModels = new List<IEdgeModel>();
                foreach (var portModel in model.OutputsByDisplayOrder)
                {
                    outputEdgeModels.AddRange(graphModel.GetEdgesConnections(portModel));
                }

                if (!outputEdgeModels.Any())
                    continue;

                graphModel.DeleteEdges(inputEdgeModels);
                graphModel.DeleteEdges(outputEdgeModels);

                graphModel.CreateEdge(outputEdgeModels[0].ToPort, inputEdgeModels[0].FromPort);
            }
        }
    }

    public class SetNodeEnabledStateAction : ModelAction<INodeModel, ModelState>
    {
        const string k_UndoStringSingular = "Change Node State";
        const string k_UndoStringPlural = "Change Nodes State";

        public SetNodeEnabledStateAction()
            : base(k_UndoStringSingular) {}

        public SetNodeEnabledStateAction(INodeModel[] nodeModel, ModelState state)
            : base(k_UndoStringSingular, k_UndoStringPlural, nodeModel, state) {}

        public static void DefaultReducer(State previousState, SetNodeEnabledStateAction action)
        {
            if (!action.Models.Any())
                return;

            previousState.PushUndo(action);

            foreach (var nodeModel in action.Models)
            {
                nodeModel.State = action.Value;
            }

            previousState.MarkForUpdate(UpdateFlags.GraphTopology);
        }
    }

    public class UpdateModelPropertyValueAction : BaseAction
    {
        public IGraphElementModel GraphElementModel;
        public object NewValue;
        public string Path;

        public UpdateModelPropertyValueAction()
        {
            UndoString = "Change Constant Value";
        }

        public UpdateModelPropertyValueAction(IGraphElementModel graphElementModel, PropertyPath path, object newValue) : this()
        {
            GraphElementModel = graphElementModel;
            Path = path?.ToString();
            NewValue = newValue;
        }

        public static void DefaultReducer(State previousState, UpdateModelPropertyValueAction action)
        {
            previousState.PushUndo(action);

            if (action.GraphElementModel is IPropertyVisitorNodeTarget target)
            {
                var targetTarget = target.Target;
                PropertyContainer.SetValue(ref targetTarget,  new PropertyPath(action.Path), action.NewValue);
                target.Target = targetTarget;
            }
            else
                PropertyContainer.SetValue(ref action.GraphElementModel,  new PropertyPath(action.Path), action.NewValue);

            previousState.MarkForUpdate(UpdateFlags.RequestCompilation, action.GraphElementModel);
        }
    }
}
