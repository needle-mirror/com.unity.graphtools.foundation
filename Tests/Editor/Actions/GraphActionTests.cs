using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor.VisualScripting.Editor;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model;
using UnityEditor.VisualScripting.Model.Stencils;
using UnityEditor.VisualScriptingTests.UI;
using UnityEngine;
using UnityEngine.TestTools;

// ReSharper disable AccessToStaticMemberViaDerivedType

namespace UnityEditor.VisualScriptingTests.Actions
{
    [Category("Graph")]
    [Category("Action")]
    class GraphActionTests : BaseFixture
    {
        protected override bool CreateGraphOnStartup => true;
        protected override Type CreatedGraphType => typeof(ClassStencil);

        [Test]
        public void Test_CreateFunctionAction([Values] TestingMode mode)
        {
            TestPrereqActionPostreq(mode,
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(0));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    return new CreateFunctionAction("TestFunction", Vector2.zero);
                },
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(1));
                    Assert.That(GetStackCount(), Is.EqualTo(1));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                });
        }

        [Test]
        public void Test_CreateEventFunctionAction([Values] TestingMode mode)
        {
            TestPrereqActionPostreq(mode,
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(0));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    var methodInfo = TypeSystem.GetMethod(typeof(Debug), nameof(Debug.Log), true);
                    return new CreateEventFunctionAction(methodInfo, Vector2.zero);
                },
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(1));
                    Assert.That(GetStackCount(), Is.EqualTo(1));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                });
        }

        [Test]
        public void Test_DeleteElementsAction_MiniGraph([Values] TestingMode mode)
        {
            var method = GraphModel.CreateFunction("TestFunction", Vector2.zero);
            var node0 = method.CreateStackedNode<Type0FakeNodeModel>("Fake0", 0);
            var node1 = GraphModel.CreateNode<Type0FakeNodeModel>("Fake1", Vector2.zero);
            GraphModel.CreateEdge(node0.Input0, node1.Output0);

            TestPrereqActionPostreq(mode,
                () =>
                {
                    Assert.That(GetStackCount(), Is.EqualTo(1));
                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GetEdgeCount(), Is.EqualTo(1));
                    var n0 = GetStackedNode(0, 0) as Type0FakeNodeModel;
                    var n1 = GetNode(1) as Type0FakeNodeModel;
                    Assert.NotNull(n0);
                    Assert.NotNull(n1);
                    Assert.That(n0.Input0, Is.ConnectedTo(n1.Output0));
                    return new DeleteElementsAction(method, node0, node1);
                },
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(0));
                    Assert.That(GetStackCount(), Is.EqualTo(0));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                });
        }

        [Test]
        public void Test_RenameFunctionAction([Values] TestingMode mode)
        {
            GraphModel.CreateFunction("TestFunction", Vector2.zero);

            TestPrereqActionPostreq(mode,
                () =>
                {
                    var function = GetFunctionModel("TestFunction");
                    Assert.That(function, Is.Not.Null);
                    Assert.That(GetStackCount(), Is.EqualTo(1));
                    Assert.That(GetNodeCount(), Is.EqualTo(1));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    return new RenameElementAction(function, "BetterNameFunction");
                },
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(1));
                    Assert.That(GetStackCount(), Is.EqualTo(1));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.IsNull(GetFunctionModel("TestFunction"));
                    Assert.IsNotNull(GetFunctionModel("BetterNameFunction"));
                });
        }

        [Test]
        public void Test_MoveElementsActionForNodes([Values] TestingMode mode)
        {
            var node0 = GraphModel.CreateNode<Type0FakeNodeModel>("Node0", Vector2.zero);
            var node1 = GraphModel.CreateNode<Type0FakeNodeModel>("Node1", Vector2.zero);
            var node2 = GraphModel.CreateNode<Type0FakeNodeModel>("Node2", Vector2.zero);
            var node3 = GraphModel.CreateNode<Type0FakeNodeModel>("Node3", Vector2.zero);
            var newPosition0 = new Vector2(50, -75);
            var newPosition1 = new Vector2(60, 25);
            var newPosition2 = new Vector2(-30, 15);
            var deltaAll = new Vector2(100, 100);

            TestPrereqActionPostreq(mode,
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(4));
                    Assert.That(GetNode(0).Position, Is.EqualTo(Vector2.zero));
                    Assert.That(GetNode(1).Position, Is.EqualTo(Vector2.zero));
                    Assert.That(GetNode(2).Position, Is.EqualTo(Vector2.zero));
                    Assert.That(GetNode(3).Position, Is.EqualTo(Vector2.zero));
                    return new MoveElementsAction(newPosition0, new[] {GetNode(0)}, null);
                },
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(4));
                    Assert.That(GetNode(0).Position, Is.EqualTo(newPosition0));
                    Assert.That(GetNode(1).Position, Is.EqualTo(Vector2.zero));
                    Assert.That(GetNode(2).Position, Is.EqualTo(Vector2.zero));
                    Assert.That(GetNode(3).Position, Is.EqualTo(Vector2.zero));
                });

            TestPrereqActionPostreq(mode,
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(4));
                    Assert.That(GetNode(0).Position, Is.EqualTo(newPosition0));
                    Assert.That(GetNode(1).Position, Is.EqualTo(Vector2.zero));
                    Assert.That(GetNode(2).Position, Is.EqualTo(Vector2.zero));
                    Assert.That(GetNode(3).Position, Is.EqualTo(Vector2.zero));
                    return new MoveElementsAction(newPosition1, new[] {GetNode(1)}, null);
                },
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(4));
                    Assert.That(GetNode(0).Position, Is.EqualTo(newPosition0));
                    Assert.That(GetNode(1).Position, Is.EqualTo(newPosition1));
                    Assert.That(GetNode(2).Position, Is.EqualTo(Vector2.zero));
                    Assert.That(GetNode(3).Position, Is.EqualTo(Vector2.zero));
                });

            TestPrereqActionPostreq(mode,
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(4));
                    Assert.That(GetNode(0).Position, Is.EqualTo(newPosition0));
                    Assert.That(GetNode(1).Position, Is.EqualTo(newPosition1));
                    Assert.That(GetNode(2).Position, Is.EqualTo(Vector2.zero));
                    Assert.That(GetNode(3).Position, Is.EqualTo(Vector2.zero));
                    return new MoveElementsAction(newPosition2, new[] {GetNode(2)}, null);
                },
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(4));
                    Assert.That(GetNode(0).Position, Is.EqualTo(newPosition0));
                    Assert.That(GetNode(1).Position, Is.EqualTo(newPosition1));
                    Assert.That(GetNode(2).Position, Is.EqualTo(newPosition2));
                    Assert.That(GetNode(3).Position, Is.EqualTo(Vector2.zero));
                });

            TestPrereqActionPostreq(mode,
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(4));
                    Assert.That(GetNode(0).Position, Is.EqualTo(newPosition0));
                    Assert.That(GetNode(1).Position, Is.EqualTo(newPosition1));
                    Assert.That(GetNode(2).Position, Is.EqualTo(newPosition2));
                    Assert.That(GetNode(3).Position, Is.EqualTo(Vector2.zero));
                    return new MoveElementsAction(deltaAll, new[] {GetNode(0), GetNode(1), GetNode(2), GetNode(3)}, null);
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
        public void Test_MoveElementsActionForStickyNodes([Values] TestingMode mode)
        {
            var origStickyPosition = new Rect(Vector2.zero, new Vector2(100, 100));
            var newStickyPosition = new Rect(Vector2.right * 100, new Vector2(100, 100));
            var stickyNote = (StickyNoteModel)GraphModel.CreateStickyNote(origStickyPosition);

            TestPrereqActionPostreq(mode,
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(0));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(GetStickyNoteCount(), Is.EqualTo(1));
                    Assert.That(GetStickyNote(0).Position, Is.EqualTo(origStickyPosition));
                    return new MoveElementsAction(newStickyPosition.position - origStickyPosition.position, null, new[] {stickyNote});
                },
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(0));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(GetStickyNoteCount(), Is.EqualTo(1));
                    Assert.That(GetStickyNote(0).Position.position, Is.EqualTo(newStickyPosition.position));
                });
        }

        [Test]
        public void Test_MoveElementsActionForMultipleTypes([Values] TestingMode mode)
        {
            var deltaMove = new Vector2(50, -75);
            var itemSize = new Vector2(100, 100);

            var origNodePosition = Vector2.zero;
            var newNodePosition = deltaMove;
            var node = GraphModel.CreateNode<Type0FakeNodeModel>("Node0", Vector2.zero);

            var origStickyPosition = new Rect(Vector2.one * -100, itemSize);
            var newStickyPosition = new Rect(origStickyPosition.position + deltaMove, itemSize);
            var stickyNote = (StickyNoteModel)GraphModel.CreateStickyNote(origStickyPosition);

            TestPrereqActionPostreq(mode,
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(1));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(GetStickyNoteCount(), Is.EqualTo(1));
                    Assert.That(GetNode(0).Position, Is.EqualTo(origNodePosition));
                    Assert.That(GetStickyNote(0).Position, Is.EqualTo(origStickyPosition));
                    return new MoveElementsAction(deltaMove, new[] {node}, new[] {stickyNote});
                },
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(1));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(GetStickyNoteCount(), Is.EqualTo(1));
                    Assert.That(GetNode(0).Position, Is.EqualTo(newNodePosition));
                    Assert.That(GetStickyNote(0).Position.position, Is.EqualTo(newStickyPosition.position));
                });
        }
    }

    class GraphActionUITests : BaseUIFixture
    {
        protected override bool CreateGraphOnStartup => true;
        protected override Type CreatedGraphType => typeof(ClassStencil);

        bool m_Changed;

        [UnityTest]
        public IEnumerator PanToNodeChangesViewTransform()
        {
            BinaryOperatorNodeModel operatorModel = GraphModel.CreateBinaryOperatorNode(BinaryOperatorKind.Add, new Vector2(-100, -100));
            var stackModel0 = GraphModel.CreateStack("Stack 1", new Vector2(100, -100));
            var stackModel1 = GraphModel.CreateStack("Stack 2", new Vector2(100, 100));
            GraphModel.CreateEdge(stackModel1.InputPorts[0], stackModel0.OutputPorts[0]);
            var nodeA = stackModel0.CreateStackedNode<Type0FakeNodeModel>("A");
            var nodeB = stackModel0.CreateStackedNode<Type0FakeNodeModel>("B");

            Store.Dispatch(new RefreshUIAction(UpdateFlags.All));
            yield return null;

            GraphView.viewTransformChanged += view => m_Changed = true;

            yield return SendPanToNodeAndRefresh(stackModel0);
            yield return SendPanToNodeAndRefresh(stackModel1);
            yield return SendPanToNodeAndRefresh(operatorModel);
            yield return SendPanToNodeAndRefresh(nodeA);
            yield return SendPanToNodeAndRefresh(nodeB);

            IEnumerator SendPanToNodeAndRefresh(NodeModel nodeModel)
            {
                m_Changed = false;

                Store.Dispatch(new PanToNodeAction(nodeModel.Guid));
                yield return null;
                Store.Dispatch(new RefreshUIAction(UpdateFlags.All));
                yield return null;

                Assert.IsTrue(m_Changed, "ViewTransform didn't change");
                Assert.That(GraphView.selection.
                    OfType<IHasGraphElementModel>().
                    Where(x => x.GraphElementModel is INodeModel n && n.Guid == nodeModel.Guid).Any,
                    () =>
                    {
                        var graphViewSelection = String.Join(",", GraphView.selection);
                        return $"Selection doesn't contain {nodeModel} {nodeModel.Title} but {graphViewSelection}";
                    });
            }
        }

        [UnityTest]
        public IEnumerator RefreshUIPreservesSelection()
        {
            var stackModel0 = GraphModel.CreateStack("A", new Vector2(100, -100));
            var stackModel1 = GraphModel.CreateStack("B", new Vector2(100, 100));

            Store.Dispatch(new RefreshUIAction(UpdateFlags.All));
            yield return null;

            GraphView.ClearSelection();
            GraphView.AddToSelection(GraphView.UIController.ModelsToNodeMapping[stackModel0]);
            yield return SendPanToNodeAndRefresh(stackModel0);
            GraphView.ClearSelection();
            GraphView.AddToSelection(GraphView.UIController.ModelsToNodeMapping[stackModel1]);
            yield return SendPanToNodeAndRefresh(stackModel1);

            IEnumerator SendPanToNodeAndRefresh(NodeModel nodeModel)
            {
                Store.Dispatch(new RefreshUIAction(UpdateFlags.All));
                yield return null;
                Assert.That(GraphView.selection.
                    OfType<IHasGraphElementModel>().
                    Where(x => x.GraphElementModel is INodeModel n && n.Guid == nodeModel.Guid).Any,
                    () =>
                    {
                        var graphViewSelection = String.Join(",", GraphView.selection.Select(x =>
                            x is IHasGraphElementModel hasModel ? hasModel.GraphElementModel.ToString() : x.ToString()));
                        return $"Selection doesn't contain {nodeModel} {nodeModel.Title} but {graphViewSelection}";
                    });
            }
        }
    }
}
