using System.Collections.Generic;
using UnityEditor.GraphToolsFoundation.Overdrive.GraphElements;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GraphElements.Utilities
{
    public class TestGraphView : GraphView
    {
        public readonly ContentDragger contentDragger;
        public readonly SelectionDragger selectionDragger;
        public readonly RectangleSelector rectangleSelector;
        public readonly FreehandSelector freehandSelector;

        public TestGraphView(IStore store) : base(store)
        {
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

        public void RebuildUI(BasicGraphModel graphModel, IStore store)
        {
            var nodeList = nodes.ToList();
            foreach (var node in nodeList)
            {
                RemoveElement(node);
            }

            var edgeList = edges.ToList();
            foreach (var edge in edgeList)
            {
                RemoveElement(edge);
            }

            var stickyList = stickies.ToList();
            foreach (var sticky in stickyList)
            {
                RemoveElement(sticky);
            }

            var placematList = placematContainer.Placemats;
            foreach (var placemat in placematList)
            {
                RemoveElement(placemat);
            }
            placematContainer.RemoveAllPlacemats();

            GraphElementFactory.RemoveAll(this);

            foreach (var nodeModel in graphModel.Nodes)
            {
                var element = GraphElementFactory.CreateUI<GraphElement>(this, store, nodeModel);
                AddElement(element);
            }

            foreach (var edgeModel in graphModel.Edges)
            {
                var element = GraphElementFactory.CreateUI<GraphElement>(this, store, edgeModel);
                AddElement(element);
            }

            foreach (var stickyNoteModel in graphModel.Stickies)
            {
                var element = GraphElementFactory.CreateUI<GraphElement>(this, store, stickyNoteModel);
                AddElement(element);
            }

            List<IGraphElement> placemats = new List<IGraphElement>();
            foreach (var placematModel in graphModel.Placemats)
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
