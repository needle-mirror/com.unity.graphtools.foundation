using UnityEditor.GraphToolsFoundation.Overdrive.Model;

namespace UnityEditor.GraphToolsFoundation.Overdrive.GraphElements
{
    [GraphElementsExtensionMethodsCache]
    public static class DefaultFactoryExtensions
    {
        public static IGraphElement CreateCollapsiblePortNode(this ElementBuilder elementBuilder, Store store, IGTFNodeModel model)
        {
            IGraphElement ui;

            if (model is ISingleInputPortNode || model is ISingleOutputPortNode)
                ui = new TokenNode();
            else if (model is IPortNode)
                ui = new CollapsibleInOutNode();
            else
                ui = new Node();

            ui.SetupBuildAndUpdate(model, store, elementBuilder.GraphView);
            return ui;
        }

        public static IGraphElement CreatePort(this ElementBuilder elementBuilder, Store store, IGTFPortModel model)
        {
            var ui = new Port();
            ui.SetupBuildAndUpdate(model, store, elementBuilder.GraphView);
            return ui;
        }

        public static IGraphElement CreateEdge(this ElementBuilder elementBuilder, Store store, IGTFEdgeModel model)
        {
            var ui = new Edge();
            ui.SetupBuildAndUpdate(model, store, elementBuilder.GraphView);
            return ui;
        }

        public static IGraphElement CreateStickyNote(this ElementBuilder elementBuilder, Store store, IGTFStickyNoteModel model)
        {
            var ui = new StickyNote();
            ui.SetupBuildAndUpdate(model, store, elementBuilder.GraphView);
            return ui;
        }

        public static IGraphElement CreatePlacemat(this ElementBuilder elementBuilder, Store store, IGTFPlacematModel model)
        {
            var ui = new Placemat();
            ui.SetupBuildAndUpdate(model, store, elementBuilder.GraphView);
            return ui;
        }

        public static IGraphElement CreateEdgePortal(this ElementBuilder elementBuilder, Store store, IGTFEdgePortalModel model)
        {
            var ui = new TokenNode();
            ui.SetupBuildAndUpdate(model, store, elementBuilder.GraphView);
            return ui;
        }
    }
}
