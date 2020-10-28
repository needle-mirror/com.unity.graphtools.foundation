using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GraphElements
{
    class TestGraphView : GraphView
    {
        public readonly ContentDragger contentDragger;
        public readonly SelectionDragger selectionDragger;
        public readonly RectangleSelector rectangleSelector;
        public readonly FreehandSelector freehandSelector;

        public TestGraphView(GraphViewEditorWindow window, Store store) : base(window, store)
        {
            // This is needed for selection persistence.
            viewDataKey = "TestGraphView";

            name = "TestGraphView";

            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);

            contentDragger = new ContentDragger();
            selectionDragger = new SelectionDragger(this);
            rectangleSelector = new RectangleSelector();
            freehandSelector = new FreehandSelector();

            this.AddManipulator(contentDragger);
            this.AddManipulator(selectionDragger);
            this.AddManipulator(rectangleSelector);
            this.AddManipulator(freehandSelector);

            Insert(0, new GridBackground());

            focusable = true;
        }

        public void RebuildUI(IGraphModel graphModel, Store store)
        {
            var nodeList = Nodes.ToList();
            foreach (var node in nodeList)
            {
                RemoveElement(node);
            }

            var edgeList = Edges.ToList();
            foreach (var edge in edgeList)
            {
                RemoveElement(edge);
            }

            var stickyList = Stickies.ToList();
            foreach (var sticky in stickyList)
            {
                RemoveElement(sticky);
            }

            var placematList = PlacematContainer.Placemats;
            foreach (var placemat in placematList)
            {
                RemoveElement(placemat);
            }
            PlacematContainer.RemoveAllPlacemats();

            GraphElementFactory.RemoveAll(this);

            foreach (var nodeModel in graphModel.NodeModels)
            {
                var element = GraphElementFactory.CreateUI<GraphElement>(this, store, nodeModel);
                AddElement(element);
            }

            foreach (var edgeModel in graphModel.EdgeModels)
            {
                var element = GraphElementFactory.CreateUI<GraphElement>(this, store, edgeModel);
                AddElement(element);
            }

            foreach (var stickyNoteModel in graphModel.StickyNoteModels)
            {
                var element = GraphElementFactory.CreateUI<GraphElement>(this, store, stickyNoteModel);
                AddElement(element);
            }

            List<IGraphElement> placemats = new List<IGraphElement>();
            foreach (var placematModel in graphModel.PlacematModels)
            {
                placemats.Add(GraphElementFactory.CreateUI<GraphElement>(this, store, placematModel));
            }

            // Update placemats to make sure hidden elements are all hidden (since
            // a placemat can hide a placemat UI created after itself).
            foreach (var placemat in placemats)
            {
                placemat.UpdateFromModel();
            }
        }
    }
}
