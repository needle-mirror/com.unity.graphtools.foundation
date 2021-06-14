using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GraphElements
{
    class TestGraphView : GraphView
    {
        public SelectionDragger TestSelectionDragger => SelectionDragger;

        public TestGraphView(GraphViewEditorWindow window, CommandDispatcher commandDispatcher)
            : base(window, commandDispatcher, "")
        {
        }

        public bool DisplaySmartSearchCalled { get; set; }
        public override void DisplaySmartSearch(Vector2 mousePosition)
        {
            DisplaySmartSearchCalled = true;
            base.DisplaySmartSearch(mousePosition);
        }

        // If you need this, ask yourself if you should not use a UnityTest and CommandDispatcher.State.GraphViewState.RequestUIRebuild();
        public void RebuildUI(IGraphModel graphModel, CommandDispatcher commandDispatcher)
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

            var placematList = Placemats.ToList();
            foreach (var placemat in placematList)
            {
                RemoveElement(placemat);
            }

            UIForModel.Reset();

            foreach (var nodeModel in graphModel.NodeModels)
            {
                var element = GraphElementFactory.CreateUI<GraphElement>(this, commandDispatcher, nodeModel);
                AddElement(element);
            }

            foreach (var edgeModel in graphModel.EdgeModels)
            {
                var element = GraphElementFactory.CreateUI<GraphElement>(this, commandDispatcher, edgeModel);
                AddElement(element);
            }

            foreach (var stickyNoteModel in graphModel.StickyNoteModels)
            {
                var element = GraphElementFactory.CreateUI<GraphElement>(this, commandDispatcher, stickyNoteModel);
                AddElement(element);
            }

            List<IModelUI> placemats = new List<IModelUI>();
            foreach (var placematModel in graphModel.PlacematModels)
            {
                var element = GraphElementFactory.CreateUI<GraphElement>(this, commandDispatcher, placematModel);
                AddElement(element);
                placemats.Add(element);
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
