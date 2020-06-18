using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.GraphElements;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using UnityEditor.GraphToolsFoundation.Overdrive.Tests.Stylesheets;
using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting;
using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.GraphViewModel;
using UnityEngine;
using UnityEngine.TestTools;
using Node = UnityEditor.GraphToolsFoundation.Overdrive.GraphElements.Node;

// ReSharper disable AccessToStaticMemberViaDerivedType

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.UI
{
    class PlacematTests : BaseUIFixture
    {
        protected override bool CreateGraphOnStartup => true;
        protected override Type CreatedGraphType => typeof(ClassStencil);

        // If Placemat1 is collapsed, Node is hidden. Moving Placemat2
        // should not move the node.
        //
        //        +----------------+   +-----------------+
        //        |  Placemat1     |   |   Placemat2     |
        //        |                |   |                 |
        //        |            +----------+              |
        //        |            | Node     |              |
        //        |            |          |              |
        //        |            +----------+              |
        //        |                |   |                 |
        //        |                |   |                 |
        //        +----------------+   +-----------------+
        //
        [UnityTest]
        public IEnumerator CollapsingAPlacematHidesItsContentToOtherPlacemats([Values] TestingMode mode)
        {
            GraphView.AddToClassList("CollapsingAPlacematHidesItsContentToOtherPlacemats");
            StylesheetsHelper.AddTestStylesheet(GraphView, "Tests.uss");

            GraphModel.CreatePlacemat(string.Empty, new Rect(0, 0, 200, 200));
            GraphModel.CreatePlacemat(string.Empty, new Rect(205, 0, 200, 200));
            var nodeModel = GraphModel.CreateNode<Type0FakeNodeModel>("Node0", new Vector2(190, 100));

            Store.Dispatch(new RefreshUIAction(UpdateFlags.All));
            yield return null;

            var nodeUI = nodeModel.GetUI<Node>(GraphView);
            nodeUI.style.width = 200;
            nodeUI.style.height = 200;
            yield return null;

            IEnumerable<IGraphElementModel> modelsToMove = null;
            HashSet<GraphElement> elementsToMove = new HashSet<GraphElement>();

            yield return TestPrereqActionPostreq(mode,
                () =>
                {
                    var placemat0 = GetGraphElementModel(0) as PlacematModel;
                    var placemat1 = GetGraphElementModel(1) as PlacematModel;
                    var node = GetGraphElementModel(2) as Type0FakeNodeModel;

                    Assert.IsNotNull(placemat0);
                    Assert.IsNotNull(placemat1);
                    Assert.IsNotNull(node);

                    Assert.IsFalse(placemat0.Collapsed, "Placemat0 is collapsed");
                    Assert.IsFalse(placemat1.Collapsed, "Placemat1 is collapsed");

                    var placematElement = GetGraphElements().
                        OfType<VisualScripting.Placemat>().FirstOrDefault(e => e.GraphElementModel.GetId() == placemat0.GetId());
                    elementsToMove.Clear();
                    placematElement.GetElementsToMove(false, elementsToMove);
                    modelsToMove = elementsToMove.OfType<IHasGraphElementModel>().Select(e => e.GraphElementModel);
                    Assert.IsTrue(modelsToMove.Contains(node), "Placemat0 models-to-move does not contain node");

                    placematElement = GetGraphElements().
                        OfType<VisualScripting.Placemat>().FirstOrDefault(e => e.GraphElementModel.GetId() == placemat1.GetId());
                    elementsToMove.Clear();
                    placematElement.GetElementsToMove(false, elementsToMove);
                    modelsToMove = elementsToMove.OfType<IHasGraphElementModel>().Select(e => e.GraphElementModel);
                    Assert.IsTrue(modelsToMove.Contains(node), "Placemat1 models-to-move does not contain node");
                },
                frame =>
                {
                    switch (frame)
                    {
                        case 0:
                            Store.Dispatch(new RefreshUIAction(UpdateFlags.All));
                            return TestPhase.WaitForNextFrame;
                        case 1:
                            {
                                var placemat0 = GetGraphElementModel(0) as PlacematModel;
                                var node = GetGraphElementModel(2) as IGTFGraphElementModel;
                                Store.Dispatch(new ExpandOrCollapsePlacematAction(true, new[] { node }, placemat0));
                                return TestPhase.WaitForNextFrame;
                            }
                        case 2:
                            Store.Dispatch(new RefreshUIAction(UpdateFlags.All));
                            return TestPhase.WaitForNextFrame;
                        default:
                            return TestPhase.Done;
                    }
                },
                () =>
                {
                    var placemat0 = GetGraphElementModel(0) as PlacematModel;
                    var placemat1 = GetGraphElementModel(1) as PlacematModel;
                    var node = GetGraphElementModel(2) as Type0FakeNodeModel;

                    Assert.IsNotNull(placemat0);
                    Assert.IsNotNull(placemat1);
                    Assert.IsNotNull(node);

                    Assert.IsTrue(placemat0.Collapsed, "Placemat0 is not collapsed");
                    Assert.IsFalse(placemat1.Collapsed, "Placemat1 is collapsed");

                    Assert.IsTrue(placemat0.HiddenElementsGuid.Contains(node.GetId()), "Placemat0 is not hiding node.");

                    var placematElement = GetGraphElements().
                        OfType<VisualScripting.Placemat>().FirstOrDefault(e => e.GraphElementModel.GetId() == placemat1.GetId());

                    elementsToMove.Clear();
                    placematElement.GetElementsToMove(false, elementsToMove);
                    modelsToMove = elementsToMove.OfType<IHasGraphElementModel>().Select(e => e.GraphElementModel);
                    Assert.IsFalse(modelsToMove.Contains(node), "Placemat1 models-to-move contains node");
                });
        }

        // Moving a node under a collapsed placemat and rebuilding the UI should not hide the node
        // (the placemat should not think it should hide the node because it is in its uncollapsed area).
        //
        //        +----------------+
        //        |  Placemat      |
        //        +----------------+
        //        .                .
        //        .      <<-->>    .
        //        .   +----------+ .
        //        .   | Node     | .
        //        .   |          | .
        //        .   +----------+ .
        //        .                .
        //        . . . . .  . . . .
        //
        [UnityTest]
        public IEnumerator MovingANodeUnderACollapsedPlacematShouldNotHideIt([Values] TestingMode mode)
        {
            {
                // Create a placemat and collapse it.
                var placemat = GraphModel.CreatePlacemat(string.Empty, new Rect(0, 0, 200, 500)) as PlacematModel;
                Store.Dispatch(new RefreshUIAction(UpdateFlags.All));
                yield return null;

                Store.Dispatch(new ExpandOrCollapsePlacematAction(true, new IGTFGraphElementModel[] {}, placemat));
                yield return null;

                // Add a node under it.
                GraphModel.CreateNode<Type0FakeNodeModel>("Node0", new Vector2(10, 100));
                Store.Dispatch(new RefreshUIAction(UpdateFlags.All));
                yield return null;
            }

            yield return TestPrereqActionPostreq(mode,
                () =>
                {
                    var placemat = GetGraphElementModel(0) as PlacematModel;
                    var node = GetGraphElementModel(1) as Type0FakeNodeModel;

                    Assert.IsNotNull(placemat);
                    Assert.IsNotNull(node);

                    Assert.IsTrue(placemat.Collapsed, "Placemat is not collapsed");
                    Assert.IsFalse(placemat.HiddenElementsGuid.Contains(node.GetId()), "Placemat is hiding node.");
                },
                frame =>
                {
                    switch (frame)
                    {
                        case 0:
                            Store.Dispatch(new RefreshUIAction(UpdateFlags.All));
                            return TestPhase.WaitForNextFrame;
                        case 1:
                            {
                                var node = GetGraphElementModel(1) as NodeModel;
                                Store.Dispatch(new MoveElementsAction(new Vector2(10, 0), new[] { node }));
                                return TestPhase.WaitForNextFrame;
                            }
                        case 2:
                            Store.Dispatch(new RefreshUIAction(UpdateFlags.All));
                            return TestPhase.WaitForNextFrame;
                        default:
                            return TestPhase.Done;
                    }
                },
                () =>
                {
                    var placemat = GetGraphElementModel(0) as PlacematModel;
                    var node = GetGraphElementModel(1) as Type0FakeNodeModel;

                    Assert.IsNotNull(placemat);
                    Assert.IsNotNull(node);

                    Assert.IsTrue(placemat.Collapsed, "Placemat is not collapsed");
                    Assert.IsFalse(placemat.HiddenElementsGuid.Contains(node.GetId()), "Placemat is hiding node.");
                }
            );
        }

        // If a node is under a collapsed placemat and the placemat is uncollapsed, the node will
        // fall under the placemat power. Undoing the uncollapse should liberate the node, not hide it.
        //
        //        +----------------+
        //        |  Placemat      |
        //        +----------------+
        //        .                .
        //        .                .
        //        .   +----------+ .
        //        .   | Node     | .
        //        .   |          | .
        //        .   +----------+ .
        //        .                .
        //        . . . . .  . . . .
        //
        [UnityTest]
        public IEnumerator UndoUncollapseShouldLiberateNodeUnderPlacemat([Values] TestingMode mode)
        {
            {
                // Create a placemat and collapse it.
                var placemat = GraphModel.CreatePlacemat(string.Empty, new Rect(0, 0, 200, 500)) as PlacematModel;
                Store.Dispatch(new RefreshUIAction(UpdateFlags.All));
                yield return null;

                Store.Dispatch(new ExpandOrCollapsePlacematAction(true, new IGTFGraphElementModel[] {}, placemat));
                yield return null;

                // Add a node under it.
                GraphModel.CreateNode<Type0FakeNodeModel>("Node0", new Vector2(10, 100));
                Store.Dispatch(new RefreshUIAction(UpdateFlags.All));
                yield return null;
            }

            yield return TestPrereqActionPostreq(mode,
                () =>
                {
                    var placemat = GetGraphElementModel(0) as PlacematModel;
                    var node = GetGraphElementModel(1) as Type0FakeNodeModel;

                    Assert.IsNotNull(placemat);
                    Assert.IsNotNull(node);

                    Assert.IsTrue(placemat.Collapsed, "Placemat is not collapsed");
                    Assert.IsFalse(placemat.HiddenElementsGuid.Contains(node.GetId()), "Placemat is hiding node.");
                },
                frame =>
                {
                    switch (frame)
                    {
                        case 0:
                            Store.Dispatch(new RefreshUIAction(UpdateFlags.All));
                            return TestPhase.WaitForNextFrame;
                        case 1:
                            {
                                var placemat = GetGraphElementModel(0) as PlacematModel;
                                Store.Dispatch(new ExpandOrCollapsePlacematAction(false, new IGTFGraphElementModel[] {}, placemat));
                                return TestPhase.WaitForNextFrame;
                            }
                        case 2:
                            Store.Dispatch(new RefreshUIAction(UpdateFlags.All));
                            return TestPhase.WaitForNextFrame;
                        default:
                            return TestPhase.Done;
                    }
                },
                () =>
                {
                    var placemat = GetGraphElementModel(0) as PlacematModel;
                    var node = GetGraphElementModel(1) as Type0FakeNodeModel;

                    Assert.IsNotNull(placemat);
                    Assert.IsNotNull(node);

                    Assert.IsFalse(placemat.Collapsed, "Placemat is collapsed");
                    Assert.IsTrue(placemat.HiddenElementsGuid == null || placemat.HiddenElementsGuid.Count == 0, "Placemat is hiding something.");
                }
            );
        }

        [Test(Description = "Test for VSB-865: Sticky Note doesn't collapse with placemats")]
        public void ElementIsHidableByPlacement()
        {
            var placemat = GraphModel.CreatePlacemat(string.Empty, new Rect(0, 0, 200, 500)) as PlacematModel;
            var node = GraphModel.CreateNode<Type0FakeNodeModel>("Node0", new Vector2(10, 100));
            var sticky = (StickyNoteModel)GraphModel.CreateStickyNote(new Rect(100, 100, 50, 50));

            placemat.HiddenElements = new[] {node};
            Assert.AreEqual(1, placemat.HiddenElements.Count(), "Node was not properly added to the list of hidden elements");
            Assert.AreEqual(node, placemat.HiddenElements.First(), "Placemat does not contain the expected node in its hidden elements");

            placemat.HiddenElements = new[] {sticky};
            Assert.AreEqual(1, placemat.HiddenElements.Count(), "Sticky note was not properly added to the list of hidden elements");
            Assert.AreEqual(sticky, placemat.HiddenElements.First(), "Placemat does not contain the expected sticky note in its hidden elements");
        }
    }
}
