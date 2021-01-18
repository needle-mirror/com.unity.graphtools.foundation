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

        public static void DefaultReducer(State state, SetNodeCollapsedAction action)
        {
            if (!action.Models.Any())
                return;

            state.PushUndo(action);

            foreach (var model in action.Models.OfType<ICollapsible>())
            {
                model.Collapsed = action.Value;
            }

            state.MarkChanged(action.Models.OfType<ICollapsible>().OfType<IGraphElementModel>());
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

        public static void DefaultReducer(State state, RenameElementAction action)
        {
            state.PushUndo(action);

            action.RenamableModel.Rename(action.ElementName);

            var graphModel = state.GraphModel;

            if (action.RenamableModel is IVariableDeclarationModel variableDeclarationModel)
            {
                var references = graphModel.FindReferencesInGraph<IVariableNodeModel>(variableDeclarationModel);

                state.MarkChanged(references);
                state.MarkChanged(variableDeclarationModel);
            }
            else if (action.RenamableModel is IVariableNodeModel variableModel)
            {
                variableDeclarationModel = variableModel.VariableDeclarationModel;
                var references = graphModel.FindReferencesInGraph<IVariableNodeModel>(variableDeclarationModel);

                state.MarkChanged(references);
                state.MarkChanged(variableDeclarationModel);
            }
            else if (action.RenamableModel is IEdgePortalModel edgePortalModel)
            {
                var declarationModel = edgePortalModel.DeclarationModel as IGraphElementModel;
                var references = graphModel.FindReferencesInGraph<IEdgePortalModel>(edgePortalModel.DeclarationModel);

                state.MarkChanged(references);
                state.MarkChanged(declarationModel);
            }
            else
            {
                state.MarkChanged(action.RenamableModel as IGraphElementModel);
            }
        }
    }

    public class UpdateConstantNodeValueAction : BaseAction
    {
        public IConstant Constant;
        public IGraphElementModel OwnerModel;
        public object Value;

        public UpdateConstantNodeValueAction()
        {
            UndoString = "Update Node Value";
        }

        public UpdateConstantNodeValueAction(IConstant constant, object value, IGraphElementModel owner) : this()
        {
            Constant = constant;
            Value = value;
            OwnerModel = owner;
        }

        public static void DefaultReducer(State state, UpdateConstantNodeValueAction action)
        {
            state.PushUndo(action);

            action.Constant.ObjectValue = action.Value;
            if (action.OwnerModel != null)
            {
                state.MarkChanged(action.OwnerModel);
            }
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

        public static void DefaultReducer(State state, UpdatePortConstantAction action)
        {
            state.PushUndo(action);

            if (action.PortModel.EmbeddedValue is IStringWrapperConstantModel stringWrapperConstantModel)
                stringWrapperConstantModel.StringValue = (string)action.NewValue;
            else
                action.PortModel.EmbeddedValue.ObjectValue = action.NewValue;

            state.MarkChanged(action.PortModel);
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

        public static void DefaultReducer(State state, DisconnectNodeAction action)
        {
            if (!action.Models.Any())
                return;

            state.PushUndo(action);

            var graphModel = state.GraphModel;

            foreach (var nodeModel in action.Models)
            {
                var edgeModels = graphModel.GetEdgesConnections(nodeModel).ToList();
                graphModel.DeleteEdges(edgeModels);
                state.MarkDeleted(edgeModels);
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

        public static void DefaultReducer(State state, BypassNodesAction action)
        {
            state.PushUndo(action);

            var graphModel = state.GraphModel;

            foreach (var model in action.NodesToBypass)
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

                var edge = graphModel.CreateEdge(outputEdgeModels[0].ToPort, inputEdgeModels[0].FromPort);

                state.MarkDeleted(inputEdgeModels);
                state.MarkDeleted(outputEdgeModels);
                state.MarkNew(edge);
            }

            var deletedModels = graphModel.DeleteNodes(action.Models, deleteConnections: false);
            state.MarkDeleted(deletedModels);
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

        public static void DefaultReducer(State state, SetNodeEnabledStateAction action)
        {
            if (!action.Models.Any())
                return;

            state.PushUndo(action);

            foreach (var nodeModel in action.Models)
            {
                nodeModel.State = action.Value;
            }
            state.MarkChanged(action.Models);
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

        public static void DefaultReducer(State state, UpdateModelPropertyValueAction action)
        {
            state.PushUndo(action);

            if (action.GraphElementModel is IPropertyVisitorNodeTarget target)
            {
                var targetTarget = target.Target;
                PropertyContainer.SetValue(ref targetTarget,  new PropertyPath(action.Path), action.NewValue);
                target.Target = targetTarget;
            }
            else
                PropertyContainer.SetValue(ref action.GraphElementModel,  new PropertyPath(action.Path), action.NewValue);

            if (action.GraphElementModel is INodeModel nodeModel)
                nodeModel.DefineNode();

            state.MarkChanged(action.GraphElementModel);
        }
    }
}
