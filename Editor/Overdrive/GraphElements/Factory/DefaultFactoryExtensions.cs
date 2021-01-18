using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    [GraphElementsExtensionMethodsCache(GraphElementsExtensionMethodsCacheAttribute.lowestPriority)]
    public static class DefaultFactoryExtensions
    {
        public static IGraphElement CreateNode(this ElementBuilder elementBuilder, Store store, INodeModel model)
        {
            IGraphElement ui;

            if (model is ISingleInputPortNode || model is ISingleOutputPortNode)
                ui = new TokenNode();
            else if (model is IPortNode)
                ui = new CollapsibleInOutNode();
            else
                ui = new Node();

            ui.SetupBuildAndUpdate(model, store, elementBuilder.GraphView, elementBuilder.Context);
            return ui;
        }

        public static IGraphElement CreatePort(this ElementBuilder elementBuilder, Store store, IPortModel model)
        {
            var ui = new Port();
            ui.SetupBuildAndUpdate(model, store, elementBuilder.GraphView, elementBuilder.Context);
            return ui;
        }

        public static IGraphElement CreateEdge(this ElementBuilder elementBuilder, Store store, IEdgeModel model)
        {
            var ui = new Edge();
            ui.SetupBuildAndUpdate(model, store, elementBuilder.GraphView, elementBuilder.Context);
            return ui;
        }

        public static IGraphElement CreateStickyNote(this ElementBuilder elementBuilder, Store store, IStickyNoteModel model)
        {
            var ui = new StickyNote();
            ui.SetupBuildAndUpdate(model, store, elementBuilder.GraphView, elementBuilder.Context);
            return ui;
        }

        public static IGraphElement CreatePlacemat(this ElementBuilder elementBuilder, Store store, IPlacematModel model)
        {
            var ui = new Placemat();
            ui.SetupBuildAndUpdate(model, store, elementBuilder.GraphView, elementBuilder.Context);
            return ui;
        }

        public static IGraphElement CreateEdgePortal(this ElementBuilder elementBuilder, Store store, IEdgePortalModel model)
        {
            var ui = new TokenNode();
            ui.SetupBuildAndUpdate(model, store, elementBuilder.GraphView, elementBuilder.Context);
            return ui;
        }

        public static IGraphElement CreateVariableDeclarationModelUI(this ElementBuilder elementBuilder, Store store, IVariableDeclarationModel model)
        {
            IGraphElement ui = null;

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

            ui?.SetupBuildAndUpdate(model, store, elementBuilder.GraphView, elementBuilder.Context);
            return ui;
        }

        public static IGraphElement CreateBlackboard(this ElementBuilder elementBuilder, Store store, IBlackboardGraphModel model)
        {
            var ui = new Blackboard { Windowed = true };
            ui.SetupBuildAndUpdate(model, store, elementBuilder.GraphView, elementBuilder.Context);
            return ui;
        }

        public static IGraphElement CreateErrorBadgeModelUI(this ElementBuilder elementBuilder, Store store, IErrorBadgeModel model)
        {
            var badge = new ErrorBadge();
            badge.SetupBuildAndUpdate(model, store, elementBuilder.GraphView, elementBuilder.Context);
            return badge;
        }

        public static IGraphElement CreateValueBadgeModelUI(this ElementBuilder elementBuilder, Store store, IValueBadgeModel model)
        {
            var badge = new ValueBadge();
            badge.SetupBuildAndUpdate(model, store, elementBuilder.GraphView, elementBuilder.Context);
            return badge;
        }
    }
}
