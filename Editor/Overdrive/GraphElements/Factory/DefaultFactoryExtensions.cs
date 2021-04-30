using UnityEngine.GraphToolsFoundation.CommandStateObserver;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    [GraphElementsExtensionMethodsCache(GraphElementsExtensionMethodsCacheAttribute.lowestPriority)]
    public static class DefaultFactoryExtensions
    {
        public static IModelUI CreateContext(this ElementBuilder elementBuilder, CommandDispatcher commandDispatcher, IContextNodeModel nodeModel)
        {
            IModelUI ui = new ContextNode();

            ui.SetupBuildAndUpdate(nodeModel, commandDispatcher, elementBuilder.GraphView, elementBuilder.Context);
            return ui;
        }

        public static IModelUI CreateNode(this ElementBuilder elementBuilder, CommandDispatcher commandDispatcher, INodeModel model)
        {
            IModelUI ui;

            if (model is ISingleInputPortNodeModel || model is ISingleOutputPortNodeModel)
                ui = new TokenNode();
            else if (model is IPortNodeModel)
                ui = new CollapsibleInOutNode();
            else
                ui = new Node();

            ui.SetupBuildAndUpdate(model, commandDispatcher, elementBuilder.GraphView, elementBuilder.Context);
            return ui;
        }

        public static IModelUI CreatePort(this ElementBuilder elementBuilder, CommandDispatcher commandDispatcher, IPortModel model)
        {
            var ui = new Port();
            ui.SetupBuildAndUpdate(model, commandDispatcher, elementBuilder.GraphView, elementBuilder.Context);
            return ui;
        }

        public static IModelUI CreateEdge(this ElementBuilder elementBuilder, CommandDispatcher commandDispatcher, IEdgeModel model)
        {
            var ui = new Edge();
            ui.SetupBuildAndUpdate(model, commandDispatcher, elementBuilder.GraphView, elementBuilder.Context);
            return ui;
        }

        public static IModelUI CreateStickyNote(this ElementBuilder elementBuilder, CommandDispatcher commandDispatcher, IStickyNoteModel model)
        {
            var ui = new StickyNote();
            ui.SetupBuildAndUpdate(model, commandDispatcher, elementBuilder.GraphView, elementBuilder.Context);
            return ui;
        }

        public static IModelUI CreatePlacemat(this ElementBuilder elementBuilder, CommandDispatcher commandDispatcher, IPlacematModel model)
        {
            var ui = new Placemat();
            ui.SetupBuildAndUpdate(model, commandDispatcher, elementBuilder.GraphView, elementBuilder.Context);
            return ui;
        }

        public static IModelUI CreateEdgePortal(this ElementBuilder elementBuilder, CommandDispatcher commandDispatcher, IEdgePortalModel model)
        {
            var ui = new TokenNode();
            ui.SetupBuildAndUpdate(model, commandDispatcher, elementBuilder.GraphView, elementBuilder.Context);
            return ui;
        }

        public static IModelUI CreateVariableDeclarationModelUI(this ElementBuilder elementBuilder, CommandDispatcher commandDispatcher, IVariableDeclarationModel model)
        {
            IModelUI ui = null;

            if (elementBuilder.Context == BlackboardVariablePropertiesPart.blackboardVariablePropertiesPartCreationContext)
            {
                ui = new BlackboardVariablePropertyView();
            }
            else if (elementBuilder.Context == BlackboardVariablePart.blackboardVariablePartCreationContext)
            {
                ui = new BlackboardField();
            }
            else
            {
                ui = new BlackboardRow();
            }

            ui?.SetupBuildAndUpdate(model, commandDispatcher, elementBuilder.GraphView, elementBuilder.Context);
            return ui;
        }

        public static IModelUI CreateBlackboard(this ElementBuilder elementBuilder, CommandDispatcher commandDispatcher, IBlackboardGraphModel model)
        {
            var ui = new Blackboard { Windowed = true };
            ui.SetupBuildAndUpdate(model, commandDispatcher, elementBuilder.GraphView, elementBuilder.Context);
            return ui;
        }

        public static IModelUI CreateErrorBadgeModelUI(this ElementBuilder elementBuilder, CommandDispatcher commandDispatcher, IErrorBadgeModel model)
        {
            var badge = new ErrorBadge();
            badge.SetupBuildAndUpdate(model, commandDispatcher, elementBuilder.GraphView, elementBuilder.Context);
            return badge;
        }

        public static IModelUI CreateValueBadgeModelUI(this ElementBuilder elementBuilder, CommandDispatcher commandDispatcher, IValueBadgeModel model)
        {
            var badge = new ValueBadge();
            badge.SetupBuildAndUpdate(model, commandDispatcher, elementBuilder.GraphView, elementBuilder.Context);
            return badge;
        }
    }
}
