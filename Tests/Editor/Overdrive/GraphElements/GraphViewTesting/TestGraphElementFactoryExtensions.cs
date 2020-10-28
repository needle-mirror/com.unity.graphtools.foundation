using UnityEditor.GraphToolsFoundation.Overdrive.Tests.TestModels;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GraphElements
{
    // TODO This should not be needed once the VS extensions are moved out of GTF
    [GraphElementsExtensionMethodsCache]
    public static class TestGraphElementFactoryExtensions
    {
        static IGraphElement CreateCollapsiblePortNode(this ElementBuilder elementBuilder, Store store, NodeModel model)
        {
            IGraphElement ui;

            switch (model)
            {
                case ISingleInputPortNode _:
                case ISingleOutputPortNode _:
                    ui = new TokenNode();
                    break;
                case IPortNode _:
                    ui = new CollapsibleInOutNode();
                    break;
                default:
                    ui = new Node();
                    break;
            }

            ui.SetupBuildAndUpdate(model, store, elementBuilder.GraphView);
            return ui;
        }

        static IGraphElement CreatePort(this ElementBuilder elementBuilder, Store store, PortModel model)
        {
            var ui = new Port();
            ui.SetupBuildAndUpdate(model, store, elementBuilder.GraphView);
            return ui;
        }

        static IGraphElement CreateEdge(this ElementBuilder elementBuilder, Store store, EdgeModel model)
        {
            var ui = new Edge();
            ui.SetupBuildAndUpdate(model, store, elementBuilder.GraphView);
            return ui;
        }

        static IGraphElement CreateStickyNote(this ElementBuilder elementBuilder, Store store, StickyNoteModel model)
        {
            var ui = new StickyNote();
            ui.SetupBuildAndUpdate(model, store, elementBuilder.GraphView);
            return ui;
        }

        static IGraphElement CreatePlacemat(this ElementBuilder elementBuilder, Store store, PlacematModel model)
        {
            var ui = new Placemat();
            ui.SetupBuildAndUpdate(model, store, elementBuilder.GraphView);
            return ui;
        }
    }
}
