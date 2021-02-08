using System.Collections.Generic;
using System.Linq;
using Unity.Properties;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public class SetNodeCollapsedCommand : ModelCommand<INodeModel, bool>
    {
        const string k_CollapseUndoStringSingular = "Collapse Node";
        const string k_CollapseUndoStringPlural = "Collapse Nodes";
        const string k_ExpandUndoStringSingular = "Expand Node";
        const string k_ExpandUndoStringPlural = "Expand Nodes";

        public SetNodeCollapsedCommand()
            : base("Collapse Or Expand Node") {}

        public SetNodeCollapsedCommand(IReadOnlyList<INodeModel> models, bool value)
            : base(value ? k_CollapseUndoStringSingular : k_ExpandUndoStringSingular,
                   value ? k_CollapseUndoStringPlural : k_ExpandUndoStringPlural,
                   models, value) {}

        public static void DefaultCommandHandler(GraphToolState graphToolState, SetNodeCollapsedCommand command)
        {
            if (!command.Models.Any())
                return;

            graphToolState.PushUndo(command);

            foreach (var model in command.Models.OfType<ICollapsible>())
            {
                model.Collapsed = command.Value;
            }

            graphToolState.MarkChanged(command.Models.OfType<ICollapsible>().OfType<IGraphElementModel>());
        }
    }

    public class RenameElementCommand : Command
    {
        public IRenamable RenamableModel;
        public string ElementName;

        public RenameElementCommand()
        {
            UndoString = "Rename Element";
        }

        public RenameElementCommand(IRenamable renamableModel, string name) : this()
        {
            RenamableModel = renamableModel;
            ElementName = name;
        }

        public static void DefaultCommandHandler(GraphToolState graphToolState, RenameElementCommand command)
        {
            graphToolState.PushUndo(command);

            command.RenamableModel.Rename(command.ElementName);

            var graphModel = graphToolState.GraphModel;

            if (command.RenamableModel is IVariableDeclarationModel variableDeclarationModel)
            {
                var references = graphModel.FindReferencesInGraph<IVariableNodeModel>(variableDeclarationModel);

                graphToolState.MarkChanged(references);
                graphToolState.MarkChanged(variableDeclarationModel);
            }
            else if (command.RenamableModel is IVariableNodeModel variableModel)
            {
                variableDeclarationModel = variableModel.VariableDeclarationModel;
                var references = graphModel.FindReferencesInGraph<IVariableNodeModel>(variableDeclarationModel);

                graphToolState.MarkChanged(references);
                graphToolState.MarkChanged(variableDeclarationModel);
            }
            else if (command.RenamableModel is IEdgePortalModel edgePortalModel)
            {
                var declarationModel = edgePortalModel.DeclarationModel as IGraphElementModel;
                var references = graphModel.FindReferencesInGraph<IEdgePortalModel>(edgePortalModel.DeclarationModel);

                graphToolState.MarkChanged(references);
                graphToolState.MarkChanged(declarationModel);
            }
            else
            {
                graphToolState.MarkChanged(command.RenamableModel as IGraphElementModel);
            }
        }
    }

    public class UpdateConstantNodeValueCommand : Command
    {
        public IConstant Constant;
        public IGraphElementModel OwnerModel;
        public object Value;

        public UpdateConstantNodeValueCommand()
        {
            UndoString = "Update Node Value";
        }

        public UpdateConstantNodeValueCommand(IConstant constant, object value, IGraphElementModel owner) : this()
        {
            Constant = constant;
            Value = value;
            OwnerModel = owner;
        }

        public static void DefaultCommandHandler(GraphToolState graphToolState, UpdateConstantNodeValueCommand command)
        {
            graphToolState.PushUndo(command);

            command.Constant.ObjectValue = command.Value;
            if (command.OwnerModel != null)
            {
                graphToolState.MarkChanged(command.OwnerModel);
            }
        }
    }

    public class UpdatePortConstantCommand : Command
    {
        public IPortModel PortModel;
        public object NewValue;

        public UpdatePortConstantCommand()
        {
            UndoString = "Update Port Value";
        }

        public UpdatePortConstantCommand(IPortModel portModel, object newValue) : this()
        {
            PortModel = portModel;
            NewValue = newValue;
        }

        public static void DefaultCommandHandler(GraphToolState graphToolState, UpdatePortConstantCommand command)
        {
            graphToolState.PushUndo(command);

            if (command.PortModel.EmbeddedValue is IStringWrapperConstantModel stringWrapperConstantModel)
                stringWrapperConstantModel.StringValue = (string)command.NewValue;
            else
                command.PortModel.EmbeddedValue.ObjectValue = command.NewValue;

            graphToolState.MarkChanged(command.PortModel);
        }
    }

    public class DisconnectNodeCommand : ModelCommand<INodeModel>
    {
        const string k_UndoStringSingular = "Disconnect Node";
        const string k_UndoStringPlural = "Disconnect Nodes";

        public DisconnectNodeCommand()
            : base(k_UndoStringSingular) {}

        public DisconnectNodeCommand(params INodeModel[] nodeModels)
            : base(k_UndoStringSingular, k_UndoStringPlural, nodeModels) {}

        public static void DefaultCommandHandler(GraphToolState graphToolState, DisconnectNodeCommand command)
        {
            if (!command.Models.Any())
                return;

            graphToolState.PushUndo(command);

            var graphModel = graphToolState.GraphModel;

            foreach (var nodeModel in command.Models)
            {
                var edgeModels = graphModel.GetEdgesConnections(nodeModel).ToList();
                graphModel.DeleteEdges(edgeModels);
                graphToolState.MarkDeleted(edgeModels);
            }
        }
    }

    public class BypassNodesCommand : ModelCommand<INodeModel>
    {
        const string k_UndoStringSingular = "Delete Element";
        const string k_UndoStringPlural = "Delete Elements";

        public readonly IInOutPortsNode[] NodesToBypass;

        public BypassNodesCommand()
            : base(k_UndoStringSingular) {}

        public BypassNodesCommand(IInOutPortsNode[] nodesToBypass, INodeModel[] elementsToRemove)
            : base(k_UndoStringSingular, k_UndoStringPlural, elementsToRemove)
        {
            NodesToBypass = nodesToBypass;
        }

        public static void DefaultCommandHandler(GraphToolState graphToolState, BypassNodesCommand command)
        {
            graphToolState.PushUndo(command);

            var graphModel = graphToolState.GraphModel;

            foreach (var model in command.NodesToBypass)
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

                graphToolState.MarkDeleted(inputEdgeModels);
                graphToolState.MarkDeleted(outputEdgeModels);
                graphToolState.MarkNew(edge);
            }

            var deletedModels = graphModel.DeleteNodes(command.Models, deleteConnections: false);
            graphToolState.MarkDeleted(deletedModels);
        }
    }

    public class SetNodeEnabledStateCommand : ModelCommand<INodeModel, ModelState>
    {
        const string k_UndoStringSingular = "Change Node State";
        const string k_UndoStringPlural = "Change Nodes State";

        public SetNodeEnabledStateCommand()
            : base(k_UndoStringSingular) {}

        public SetNodeEnabledStateCommand(INodeModel[] nodeModel, ModelState state)
            : base(k_UndoStringSingular, k_UndoStringPlural, nodeModel, state) {}

        public static void DefaultCommandHandler(GraphToolState graphToolState, SetNodeEnabledStateCommand command)
        {
            if (!command.Models.Any())
                return;

            graphToolState.PushUndo(command);

            foreach (var nodeModel in command.Models)
            {
                nodeModel.State = command.Value;
            }
            graphToolState.MarkChanged(command.Models);
        }
    }

    public class UpdateModelPropertyValueCommand : Command
    {
        public IGraphElementModel GraphElementModel;
        public object NewValue;
        public string Path;

        public UpdateModelPropertyValueCommand()
        {
            UndoString = "Change Constant Value";
        }

        public UpdateModelPropertyValueCommand(IGraphElementModel graphElementModel, PropertyPath path, object newValue) : this()
        {
            GraphElementModel = graphElementModel;
            Path = path?.ToString();
            NewValue = newValue;
        }

        public static void DefaultCommandHandler(GraphToolState graphToolState, UpdateModelPropertyValueCommand command)
        {
            graphToolState.PushUndo(command);

            if (command.GraphElementModel is IPropertyVisitorNodeTarget target)
            {
                var targetTarget = target.Target;
                PropertyContainer.SetValue(ref targetTarget,  new PropertyPath(command.Path), command.NewValue);
                target.Target = targetTarget;
            }
            else
                PropertyContainer.SetValue(ref command.GraphElementModel,  new PropertyPath(command.Path), command.NewValue);

            graphToolState.MarkChanged(command.GraphElementModel);
        }
    }
}
