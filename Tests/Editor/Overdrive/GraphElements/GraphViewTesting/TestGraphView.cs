using System.Collections.Generic;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GraphElements
{
    class TestGraphView : GtfoGraphView
    {
        public SelectionDragger TestSelectionDragger => SelectionDragger;

        public TestGraphView(GraphViewEditorWindow window, Store store) : base(window, store, "")
        {
            // This is needed for selection persistence.
            viewDataKey = "TestGraphView";

            name = "TestGraphView";

            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale, 1.0f);

            ContentDragger = new ContentDragger();
            SelectionDragger = new SelectionDragger(this);
            RectangleSelector = new RectangleSelector();
            FreehandSelector = new FreehandSelector();

            Insert(0, new GridBackground());

            focusable = true;
        }

        // If you need this, ask yourself if you should not use a UnityTest and Store.State.RequestUIRebuild();
        public void RebuildUI(IGraphModel graphModel, Store store)
        {
            var nodeList = Nodes.ToList();
            foreach (var node in nodeList)
            {
                RemoveElement(node, true);
            }

            var edgeList = Edges.ToList();
            foreach (var edge in edgeList)
            {
                RemoveElement(edge, true);
            }

            var stickyList = Stickies.ToList();
            foreach (var sticky in stickyList)
            {
                RemoveElement(sticky, true);
            }

            var placematList = PlacematContainer.Placemats;
            foreach (var placemat in placematList)
            {
                RemoveElement(placemat, true);
            }
            PlacematContainer.RemoveAllPlacemats();

            UIForModel.Reset();

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
                var element = GraphElementFactory.CreateUI<GraphElement>(this, store, placematModel);
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

        public override bool CanAcceptDrop(List<ISelectableGraphElement> dragSelection)
        {
            return false;
        }

        public override bool DragUpdated(DragUpdatedEvent evt, IEnumerable<ISelectableGraphElement> dragSelection, IDropTarget dropTarget, ISelection dragSource)
        {
            return false;
        }

        public override bool DragPerform(DragPerformEvent evt, IEnumerable<ISelectableGraphElement> dragSelection, IDropTarget dropTarget, ISelection dragSource)
        {
            return false;
        }

        public override bool DragEnter(DragEnterEvent evt, IEnumerable<ISelectableGraphElement> dragSelection, IDropTarget enteredTarget, ISelection dragSource)
        {
            return false;
        }

        public override bool DragLeave(DragLeaveEvent evt, IEnumerable<ISelectableGraphElement> dragSelection, IDropTarget leftTarget, ISelection dragSource)
        {
            return false;
        }

        public override bool DragExited()
        {
            return false;
        }
    }
}
