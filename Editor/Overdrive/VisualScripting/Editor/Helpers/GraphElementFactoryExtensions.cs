using System;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEditor.GraphToolsFoundation.Overdrive.GraphElements;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting
{
    [GraphElementsExtensionMethodsCache]
    public static class GraphElementFactoryExtensions
    {
        public static IGraphElement CreateNode(this ElementBuilder elementBuilder, Store store, NodeModel model)
        {
            var ui = new Node();
            ui.SetupBuildAndUpdate(model, store, elementBuilder.GraphView);
            return ui;
        }

        public static IGraphElement CreatePort(this ElementBuilder elementBuilder, Store store, PortModel model)
        {
            var ui = new Port();
            ui.SetupBuildAndUpdate(model, store, elementBuilder.GraphView);
            return ui;
        }

        public static IGraphElement CreateEdge(this ElementBuilder elementBuilder, Store store, EdgeModel model)
        {
            var ui = new Edge();
            ui.SetupBuildAndUpdate(model, store, elementBuilder.GraphView);
            return ui;
        }

        public static IGraphElement CreateStickyNote(this ElementBuilder elementBuilder, Store store, StickyNoteModel model)
        {
            var ui = new StickyNote();
            ui.SetupBuildAndUpdate(model, store, elementBuilder.GraphView);
            return ui;
        }

        public static IGraphElement CreatePlacemat(this ElementBuilder elementBuilder, Store store, PlacematModel model)
        {
            var ui = new Placemat();
            ui.SetupBuildAndUpdate(model, store, elementBuilder.GraphView);
            return ui;
        }

        public static IGraphElement CreateToken(this ElementBuilder elementBuilder, Store store, IGTFVariableNodeModel model)
        {
            var ui = new Token();
            ui.SetupBuildAndUpdate(model, store, elementBuilder.GraphView);
            return ui;
        }

        public static IGraphElement CreateConstantToken(this ElementBuilder elementBuilder, Store store, IGTFConstantNodeModel model)
        {
            var ui = new Token();
            ui.SetupBuildAndUpdate(model, store, elementBuilder.GraphView);
            return ui;
        }

        public static IGraphElement CreateEdgePortal(this ElementBuilder elementBuilder, Store store, EdgePortalModel model)
        {
            var ui = new Token();
            ui.SetupBuildAndUpdate(model, store, elementBuilder.GraphView);
            return ui;
        }
    }
}
