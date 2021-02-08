using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEditor.GraphToolsFoundation.Overdrive.Tests.UI;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

// ReSharper disable AccessToStaticMemberViaDerivedType

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.Commands
{
    [Category("Graph")]
    [Category("Command")]
    class GraphCommandTests : BaseFixture
    {
        protected override bool CreateGraphOnStartup => true;
        protected override Type CreatedGraphType => typeof(ClassStencil);

        [Test]
        public void Test_DeleteElementsCommand_MiniGraph([Values] TestingMode mode)
        {
            var node0 = GraphModel.CreateNode<Type0FakeNodeModel>("Minus", Vector2.zero);
            var node1 = GraphModel.CreateNode<Type0FakeNodeModel>("Fake1", Vector2.zero);
            GraphModel.CreateEdge(node0.Input0, node1.Output0);

            TestPrereqCommandPostreq(mode,
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GetEdgeCount(), Is.EqualTo(1));
                    var n0 = GetNode(0) as Type0FakeNodeModel;
                    var n1 = GetNode(1) as Type0FakeNodeModel;
                    Assert.NotNull(n0);
                    Assert.NotNull(n1);
                    Assert.That(n0.Input0, Is.ConnectedTo(n1.Output0));
                    return new DeleteElementsCommand(new[] { node0, node1 });
                },
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(0));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                });
        }

        [Test]
        public void Test_MoveElementsCommandForNodes([Values] TestingMode mode)
        {
            GraphModel.CreateNode<Type0FakeNodeModel>("Node0", Vector2.zero);
            GraphModel.CreateNode<Type0FakeNodeModel>("Node1", Vector2.zero);
            GraphModel.CreateNode<Type0FakeNodeModel>("Node2", Vector2.zero);
            GraphModel.CreateNode<Type0FakeNodeModel>("Node3", Vector2.zero);
            var newPosition0 = new Vector2(50, -75);
            var newPosition1 = new Vector2(60, 25);
            var newPosition2 = new Vector2(-30, 15);
            var deltaAll = new Vector2(100, 100);

            TestPrereqCommandPostreq(mode,
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(4));
                    Assert.That(GetNode(0).Position, Is.EqualTo(Vector2.zero));
                    Assert.That(GetNode(1).Position, Is.EqualTo(Vector2.zero));
                    Assert.That(GetNode(2).Position, Is.EqualTo(Vector2.zero));
                    Assert.That(GetNode(3).Position, Is.EqualTo(Vector2.zero));
                    return new MoveElementsCommand(newPosition0, new[] {GetNode(0)});
                },
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(4));
                    Assert.That(GetNode(0).Position, Is.EqualTo(newPosition0));
                    Assert.That(GetNode(1).Position, Is.EqualTo(Vector2.zero));
                    Assert.That(GetNode(2).Position, Is.EqualTo(Vector2.zero));
                    Assert.That(GetNode(3).Position, Is.EqualTo(Vector2.zero));
                });

            TestPrereqCommandPostreq(mode,
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(4));
                    Assert.That(GetNode(0).Position, Is.EqualTo(newPosition0));
                    Assert.That(GetNode(1).Position, Is.EqualTo(Vector2.zero));
                    Assert.That(GetNode(2).Position, Is.EqualTo(Vector2.zero));
                    Assert.That(GetNode(3).Position, Is.EqualTo(Vector2.zero));
                    return new MoveElementsCommand(newPosition1, new[] {GetNode(1)});
                },
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(4));
                    Assert.That(GetNode(0).Position, Is.EqualTo(newPosition0));
                    Assert.That(GetNode(1).Position, Is.EqualTo(newPosition1));
                    Assert.That(GetNode(2).Position, Is.EqualTo(Vector2.zero));
                    Assert.That(GetNode(3).Position, Is.EqualTo(Vector2.zero));
                });

            TestPrereqCommandPostreq(mode,
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(4));
                    Assert.That(GetNode(0).Position, Is.EqualTo(newPosition0));
                    Assert.That(GetNode(1).Position, Is.EqualTo(newPosition1));
                    Assert.That(GetNode(2).Position, Is.EqualTo(Vector2.zero));
                    Assert.That(GetNode(3).Position, Is.EqualTo(Vector2.zero));
                    return new MoveElementsCommand(newPosition2, new[] {GetNode(2)});
                },
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(4));
                    Assert.That(GetNode(0).Position, Is.EqualTo(newPosition0));
                    Assert.That(GetNode(1).Position, Is.EqualTo(newPosition1));
                    Assert.That(GetNode(2).Position, Is.EqualTo(newPosition2));
                    Assert.That(GetNode(3).Position, Is.EqualTo(Vector2.zero));
                });

            TestPrereqCommandPostreq(mode,
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(4));
                    Assert.That(GetNode(0).Position, Is.EqualTo(newPosition0));
                    Assert.That(GetNode(1).Position, Is.EqualTo(newPosition1));
                    Assert.That(GetNode(2).Position, Is.EqualTo(newPosition2));
                    Assert.That(GetNode(3).Position, Is.EqualTo(Vector2.zero));
                    return new MoveElementsCommand(deltaAll, new[] {GetNode(0), GetNode(1), GetNode(2), GetNode(3)});
                },
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(4));
                    Assert.That(GetNode(0).Position, Is.EqualTo(newPosition0 + deltaAll));
                    Assert.That(GetNode(1).Position, Is.EqualTo(newPosition1 + deltaAll));
                    Assert.That(GetNode(2).Position, Is.EqualTo(newPosition2 + deltaAll));
                    Assert.That(GetNode(3).Position, Is.EqualTo(deltaAll));
                });
        }

        [Test]
        public void Test_MoveElementsCommandForStickyNodes([Values] TestingMode mode)
        {
            var origStickyPosition = new Rect(Vector2.zero, new Vector2(100, 100));
            var newStickyPosition = new Rect(Vector2.right * 100, new Vector2(100, 100));
            var stickyNote = (StickyNoteModel)GraphModel.CreateStickyNote(origStickyPosition);

            TestPrereqCommandPostreq(mode,
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(0));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(GetStickyNoteCount(), Is.EqualTo(1));
                    Assert.That(GetStickyNote(0).PositionAndSize, Is.EqualTo(origStickyPosition));
                    return new MoveElementsCommand(newStickyPosition.position - origStickyPosition.position, new[] {stickyNote});
                },
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(0));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(GetStickyNoteCount(), Is.EqualTo(1));
                    Assert.That(GetStickyNote(0).PositionAndSize.position, Is.EqualTo(newStickyPosition.position));
                });
        }

        [Test]
        public void Test_MoveElementsCommandForMultipleTypes([Values] TestingMode mode)
        {
            var deltaMove = new Vector2(50, -75);
            var itemSize = new Vector2(100, 100);

            var origNodePosition = Vector2.zero;
            var newNodePosition = deltaMove;
            var node = GraphModel.CreateNode<Type0FakeNodeModel>("Node0", Vector2.zero);

            var origStickyPosition = new Rect(Vector2.one * -100, itemSize);
            var newStickyPosition = new Rect(origStickyPosition.position + deltaMove, itemSize);
            var stickyNote = (StickyNoteModel)GraphModel.CreateStickyNote(origStickyPosition);

            var origPlacematPosition = new Rect(Vector2.one * 200, itemSize);
            var newPlacematPosition = new Rect(origPlacematPosition.position + deltaMove, itemSize);
            var placemat = (PlacematModel)GraphModel.CreatePlacemat(origPlacematPosition);
            placemat.Title = "Blah";

            TestPrereqCommandPostreq(mode,
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(1));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(GetStickyNoteCount(), Is.EqualTo(1));
                    Assert.That(GetNode(0).Position, Is.EqualTo(origNodePosition));
                    Assert.That(GetStickyNote(0).PositionAndSize, Is.EqualTo(origStickyPosition));
                    Assert.That(GetPlacemat(0).PositionAndSize, Is.EqualTo(origPlacematPosition));
                    return new MoveElementsCommand(deltaMove, new IMovable[] { node, placemat, stickyNote });
                },
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(1));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(GetStickyNoteCount(), Is.EqualTo(1));
                    Assert.That(GetNode(0).Position, Is.EqualTo(newNodePosition));
                    Assert.That(GetStickyNote(0).PositionAndSize.position, Is.EqualTo(newStickyPosition.position));
                    Assert.That(GetPlacemat(0).PositionAndSize.position, Is.EqualTo(newPlacematPosition.position));
                });
        }
    }

    class GraphCommandUITests : BaseUIFixture
    {
        protected override bool CreateGraphOnStartup => true;
        protected override Type CreatedGraphType => typeof(ClassStencil);

        bool m_Changed;

        [UnityTest]
        public IEnumerator PanToNodeChangesViewTransform()
        {
            var operatorModel = GraphModel.CreateNode<Type0FakeNodeModel>("Node0", new Vector2(-100, -100));
            var nodeA = GraphModel.CreateNode<Type0FakeNodeModel>("A");
            var nodeB = GraphModel.CreateNode<Type0FakeNodeModel>("B");

            CommandDispatcher.MarkStateDirty();
            yield return null;

            GraphView.ViewTransformChangedCallback += view => m_Changed = true;

            yield return SendPanToNodeAndRefresh(operatorModel);
            yield return SendPanToNodeAndRefresh(nodeA);
            yield return SendPanToNodeAndRefresh(nodeB);

            IEnumerator SendPanToNodeAndRefresh(NodeModel nodeModel)
            {
                m_Changed = false;

                GraphView.PanToNode(nodeModel.Guid);
                yield return null;

                Assert.IsTrue(m_Changed, "ViewTransform didn't change");
                Assert.That(GraphView.Selection.
                    OfType<IModelUI>().
                    Where(x => x.Model is INodeModel n && n.Guid == nodeModel.Guid).Any,
                    () =>
                    {
                        var graphViewSelection = String.Join(",", GraphView.Selection);
                        return $"Selection doesn't contain {nodeModel} {nodeModel.Title} but {graphViewSelection}";
                    });
            }
        }

        [UnityTest]
        public IEnumerator RefreshUIPreservesSelection()
        {
            var nodeA = GraphModel.CreateNode<Type0FakeNodeModel>("A", new Vector2(100, -100));
            var nodeB = GraphModel.CreateNode<Type0FakeNodeModel>("B", new Vector2(100, 100));

            CommandDispatcher.MarkStateDirty();
            yield return null;

            GraphView.ClearSelection();
            GraphView.AddToSelection(nodeA.GetUI<GraphElement>(GraphView));
            yield return SendPanToNodeAndRefresh(nodeA);
            GraphView.ClearSelection();
            GraphView.AddToSelection(nodeB.GetUI<GraphElement>(GraphView));
            yield return SendPanToNodeAndRefresh(nodeB);

            IEnumerator SendPanToNodeAndRefresh(NodeModel nodeModel)
            {
                CommandDispatcher.MarkStateDirty();
                yield return null;
                Assert.That(GraphView.Selection.
                    OfType<IModelUI>().
                    Where(x => x.Model is INodeModel n && n.Guid == nodeModel.Guid).Any,
                    () =>
                    {
                        var graphViewSelection = String.Join(",", GraphView.Selection.Select(x =>
                            x is IModelUI hasModel ? hasModel.Model.ToString() : x.ToString()));
                        return $"Selection doesn't contain {nodeModel} {nodeModel.Title} but {graphViewSelection}";
                    });
            }
        }

        [UnityTest]
        public IEnumerator DuplicateNodeAndEdgeCreatesEdgeToOriginalNode()
        {
            var declaration0 = GraphModel.CreateGraphVariableDeclaration(typeof(int).GenerateTypeHandle(), "decl0", ModifierFlags.None, true);

            var nodeA = GraphModel.CreateVariableNode(declaration0, new Vector2(100, -100));
            var nodeB = GraphModel.CreateNode<Type0FakeNodeModel>("A", new Vector2(100, 100));

            var edge = GraphModel.CreateEdge(nodeB.Input0, nodeA.OutputPort) as EdgeModel;

            CommandDispatcher.MarkStateDirty();
            yield return null;

            GraphView.ClearSelection();
            GraphView.AddToSelection(nodeB.GetUI<GraphElement>(GraphView));
            GraphView.AddToSelection(edge.GetUI<GraphElement>(GraphView));

            GraphView.Focus();
            using (var evt = ExecuteCommandEvent.GetPooled("Duplicate"))
            {
                evt.target = GraphView;
                GraphView.SendEvent(evt);
            }
            CommandDispatcher.MarkStateDirty();
            yield return null;

            Assert.AreEqual(3, GraphModel.NodeModels.Count);
            Assert.AreEqual(2, GraphModel.EdgeModels.Count);
            foreach (var edgeModel in GraphModel.EdgeModels)
            {
                Assert.AreEqual(nodeA.OutputPort, edgeModel.FromPort);
            }
        }
    }
}