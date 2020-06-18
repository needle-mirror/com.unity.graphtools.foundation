using System;
using UnityEditor.GraphToolsFoundation.Overdrive.GraphElements;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.GraphViewModel;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting
{
    [GraphElementsExtensionMethodsCache]
    public static class GraphElementFactoryExtensions
    {
        public static IGraphElement CreateNode(this ElementBuilder elementBuilder, IStore store, NodeModel model)
        {
            var ui = new Node();
            ui.SetupBuildAndUpdate(model, store, elementBuilder.GraphView);
            return ui;
        }

        public static IGraphElement CreatePort(this ElementBuilder elementBuilder, IStore store, PortModel model)
        {
            var ui = new Port();
            ui.SetupBuildAndUpdate(model, store, elementBuilder.GraphView);
            return ui;
        }

        public static IGraphElement CreateEdge(this ElementBuilder elementBuilder, IStore store, EdgeModel model)
        {
            var ui = new Edge();
            ui.SetupBuildAndUpdate(model, store, elementBuilder.GraphView);
            return ui;
        }

        public static IGraphElement CreateStickyNote(this ElementBuilder elementBuilder, IStore store, StickyNoteModel model)
        {
            var ui = new StickyNote();
            ui.SetupBuildAndUpdate(model, store, elementBuilder.GraphView);
            return ui;
        }

        public static IGraphElement CreatePlacemat(this ElementBuilder elementBuilder, IStore store, PlacematModel model)
        {
            var ui = new Placemat();
            ui.SetupBuildAndUpdate(model, store, elementBuilder.GraphView);
            return ui;
        }

        public static IGraphElement CreateToken(this ElementBuilder elementBuilder, IStore store, IVariableModel model)
        {
            var ui = new Token();
            ui.SetupBuildAndUpdate(model as IGTFGraphElementModel, store, elementBuilder.GraphView);
            return ui;
        }

        public static IGraphElement CreateConstantToken(this ElementBuilder elementBuilder, IStore store, IConstantNodeModel model)
        {
            var ui = new Token();
            ui.SetupBuildAndUpdate(model as IGTFGraphElementModel, store, elementBuilder.GraphView);
            return ui;
        }

        public static IGraphElement CreateToken(this ElementBuilder elementBuilder, IStore store, IStringWrapperConstantModel model)
        {
            return CreateConstantToken(elementBuilder, store, model);
        }

        public static IGraphElement CreateToken(this ElementBuilder elementBuilder, IStore store, SystemConstantNodeModel model)
        {
            var ui = new Token();
            ui.SetupBuildAndUpdate(model, store, elementBuilder.GraphView);
            return ui;
        }

        public static IGraphElement CreateEdgePortal(this ElementBuilder elementBuilder, IStore store, IEdgePortalModel model)
        {
            var ui = new Token();
            ui.SetupBuildAndUpdate(model as IGTFGraphElementModel, store, elementBuilder.GraphView);
            return ui;
        }
    }
}
