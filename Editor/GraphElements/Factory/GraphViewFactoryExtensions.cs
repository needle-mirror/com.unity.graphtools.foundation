using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Extension methods to create UI for graph element models for the <see cref="GraphView"/>.
    /// </summary>
    /// <remarks>
    /// Extension methods in this class are selected by matching the type of their third parameter to the type
    /// of the graph element model for which we need to instantiate a IModelUI. You can change the UI for a
    /// model by defining new extension methods for <see cref="ElementBuilder"/> in a class having
    /// the <see cref="GraphElementsExtensionMethodsCacheAttribute"/>.
    /// </remarks>
    [GraphElementsExtensionMethodsCache(typeof(GraphView), GraphElementsExtensionMethodsCacheAttribute.lowestPriority)]
    public static class GraphViewFactoryExtensions
    {
        public static IModelUI CreateContext(this ElementBuilder elementBuilder, CommandDispatcher commandDispatcher, IContextNodeModel nodeModel)
        {
            IModelUI ui = new ContextNode();

            ui.SetupBuildAndUpdate(nodeModel, commandDispatcher, elementBuilder.View, elementBuilder.Context);
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

            ui.SetupBuildAndUpdate(model, commandDispatcher, elementBuilder.View, elementBuilder.Context);
            return ui;
        }

        public static IModelUI CreatePort(this ElementBuilder elementBuilder, CommandDispatcher commandDispatcher, IPortModel model)
        {
            var ui = new Port();
            ui.SetupBuildAndUpdate(model, commandDispatcher, elementBuilder.View, elementBuilder.Context);
            return ui;
        }

        public static IModelUI CreateEdge(this ElementBuilder elementBuilder, CommandDispatcher commandDispatcher, IEdgeModel model)
        {
            var ui = new Edge();
            ui.SetupBuildAndUpdate(model, commandDispatcher, elementBuilder.View, elementBuilder.Context);
            return ui;
        }

        public static IModelUI CreateStickyNote(this ElementBuilder elementBuilder, CommandDispatcher commandDispatcher, IStickyNoteModel model)
        {
            var ui = new StickyNote();
            ui.SetupBuildAndUpdate(model, commandDispatcher, elementBuilder.View, elementBuilder.Context);
            return ui;
        }

        public static IModelUI CreatePlacemat(this ElementBuilder elementBuilder, CommandDispatcher commandDispatcher, IPlacematModel model)
        {
            var ui = new Placemat();
            ui.SetupBuildAndUpdate(model, commandDispatcher, elementBuilder.View, elementBuilder.Context);
            return ui;
        }

        public static IModelUI CreateEdgePortal(this ElementBuilder elementBuilder, CommandDispatcher commandDispatcher, IEdgePortalModel model)
        {
            var ui = new TokenNode();
            ui.SetupBuildAndUpdate(model, commandDispatcher, elementBuilder.View, elementBuilder.Context);
            return ui;
        }

        public static IModelUI CreateVariableDeclarationModelUI(this ElementBuilder elementBuilder, CommandDispatcher commandDispatcher, IVariableDeclarationModel model)
        {
            IModelUI ui;

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

            ui.SetupBuildAndUpdate(model, commandDispatcher, elementBuilder.View, elementBuilder.Context);
            return ui;
        }

        public static IModelUI CreateBlackboard(this ElementBuilder elementBuilder, CommandDispatcher commandDispatcher, IBlackboardGraphModel model)
        {
            var ui = new Blackboard { Windowed = true };
            ui.SetupBuildAndUpdate(model, commandDispatcher, elementBuilder.View, elementBuilder.Context);
            return ui;
        }

        public static IModelUI CreateErrorBadgeModelUI(this ElementBuilder elementBuilder, CommandDispatcher commandDispatcher, IErrorBadgeModel model)
        {
            var badge = new ErrorBadge();
            badge.SetupBuildAndUpdate(model, commandDispatcher, elementBuilder.View, elementBuilder.Context);
            return badge;
        }

        public static IModelUI CreateValueBadgeModelUI(this ElementBuilder elementBuilder, CommandDispatcher commandDispatcher, IValueBadgeModel model)
        {
            var badge = new ValueBadge();
            badge.SetupBuildAndUpdate(model, commandDispatcher, elementBuilder.View, elementBuilder.Context);
            return badge;
        }
    }
}
