using UnityEditor.GraphToolsFoundation.Overdrive.Tests.TestModels;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GraphElements
{
    // TODO This should not be needed once the VS extensions are moved out of GTF
    [GraphElementsExtensionMethodsCache]
    public static class TestGraphElementFactoryExtensions
    {
        static IModelUI CreateCollapsiblePortNode(this ElementBuilder elementBuilder, CommandDispatcher commandDispatcher, NodeModel model)
        {
            IModelUI ui;

            switch (model)
            {
                case ISingleInputPortNodeModel _:
                case ISingleOutputPortNodeModel _:
                    ui = new TokenNode();
                    break;
                case IPortNodeModel _:
                    ui = new CollapsibleInOutNode();
                    break;
                default:
                    ui = new Node();
                    break;
            }

            ui.SetupBuildAndUpdate(model, commandDispatcher, elementBuilder.GraphView, elementBuilder.Context);
            return ui;
        }

        static IModelUI CreatePort(this ElementBuilder elementBuilder, CommandDispatcher commandDispatcher, PortModel model)
        {
            var ui = new Port();
            ui.SetupBuildAndUpdate(model, commandDispatcher, elementBuilder.GraphView, elementBuilder.Context);
            return ui;
        }

        static IModelUI CreateEdge(this ElementBuilder elementBuilder, CommandDispatcher commandDispatcher, EdgeModel model)
        {
            var ui = new Edge();
            ui.SetupBuildAndUpdate(model, commandDispatcher, elementBuilder.GraphView, elementBuilder.Context);
            return ui;
        }

        static IModelUI CreateStickyNote(this ElementBuilder elementBuilder, CommandDispatcher commandDispatcher, StickyNoteModel model)
        {
            var ui = new StickyNote();
            ui.SetupBuildAndUpdate(model, commandDispatcher, elementBuilder.GraphView, elementBuilder.Context);
            return ui;
        }

        static IModelUI CreatePlacemat(this ElementBuilder elementBuilder, CommandDispatcher commandDispatcher, PlacematModel model)
        {
            var ui = new Placemat();
            ui.SetupBuildAndUpdate(model, commandDispatcher, elementBuilder.GraphView, elementBuilder.Context);
            return ui;
        }
    }
}
