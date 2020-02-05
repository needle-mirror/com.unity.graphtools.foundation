using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor.Experimental.GraphView;
using UnityEditor.VisualScripting.Editor;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model;
using UnityEditor.VisualScripting.Model.Stencils;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

// ReSharper disable AccessToStaticMemberViaDerivedType

namespace UnityEditor.VisualScriptingTests.UI
{
    class MoveDependencyTests : BaseUIFixture
    {
        protected override bool CreateGraphOnStartup => true;
        protected override Type CreatedGraphType => typeof(ClassStencil);

        [Test]
        public void DeleteNodeDoesRemoveTheDependency()
        {
            var mgr = new PositionDependenciesManager(GraphView, GraphView.window.Preferences);
            BinaryOperatorNodeModel operatorModel = GraphModel.CreateBinaryOperatorNode(BinaryOperatorKind.Add, new Vector2(-100, -100));
            IConstantNodeModel intModel = GraphModel.CreateConstantNode("int", typeof(int).GenerateTypeHandle(GraphModel.Stencil), new Vector2(-150, -100));
            var edge = GraphModel.CreateEdge(operatorModel.InputPortA, intModel.OutputPort);
            mgr.AddPositionDependency(edge);
            mgr.Remove(operatorModel.Guid, intModel.Guid);
            Assert.That(mgr.GetDependencies(operatorModel), Is.Null);
        }

        [UnityTest, Ignore("@theor needs to figure this one out")]
        public IEnumerator EndToEndMoveDependencyWithPanning()
        {
            var stackModel0 = GraphModel.CreateStack(string.Empty, new Vector2(100, -100));
            var stackModel1 = GraphModel.CreateStack(string.Empty, new Vector2(100, 100));
            GraphModel.CreateEdge(stackModel1.InputPorts[0], stackModel0.OutputPorts[0]);

            Store.Dispatch(new RefreshUIAction(UpdateFlags.All));
            yield return null;
            GraphView.FrameAll();
            yield return null;

            bool needsMouseUp = false;
            try
            {
                using (var scheduler = GraphView.CreateTimerEventSchedulerWrapper())
                {
                    GraphElement stackNode = GraphView.UIController.ModelsToNodeMapping[stackModel0];
                    Vector2 startPos = stackNode.GetPosition().position;
                    Vector2 otherStartPos = stackModel1.Position;
                    Vector2 nodeRect = stackNode.hierarchy.parent.ChangeCoordinatesTo(Window.rootVisualElement, stackNode.layout.center);

                    // Move the movable node.
                    Vector2 pos = nodeRect;
                    Vector2 target = new Vector2(Window.rootVisualElement.layout.xMax - 20, pos.y);
                    needsMouseUp = true;
                    bool changed = false;
                    GraphView.viewTransformChanged += view => changed = true;
                    Helpers.MouseDownEvent(pos);
                    yield return null;


                    Helpers.MouseMoveEvent(pos, target);
                    Helpers.MouseDragEvent(pos, target);
                    yield return null;

                    scheduler.TimeSinceStartup += GraphViewTestHelpers.SelectionDraggerPanInterval;
                    scheduler.UpdateScheduledEvents();

                    Helpers.MouseUpEvent(target);
                    needsMouseUp = false;
                    Assume.That(changed, Is.True);

                    yield return null;

                    Vector2 delta = stackNode.GetPosition().position - startPos;
                    Assert.That(stackModel1.Position, Is.EqualTo(otherStartPos + delta));
                }
            }
            finally
            {
                if (needsMouseUp)
                    Helpers.MouseUpEvent(Vector2.zero);
            }
        }

        [UnityTest]
        public IEnumerator MovingAStackMovesTheConnectedStack([Values] TestingMode mode)
        {
            var stackModel0 = GraphModel.CreateStack(string.Empty, new Vector2(-100, -100));
            var stackModel1 = GraphModel.CreateStack(string.Empty, new Vector2(100, 100));
            GraphModel.CreateEdge(stackModel1.InputPorts[0], stackModel0.OutputPorts[0]);

            yield return TestMove(mode,
                mouseDelta: new Vector2(20, 10),
                movedNodes: new INodeModel[] { stackModel0 },
                expectedMovedDependencies: new INodeModel[] { stackModel1 }
            );
        }

        [UnityTest]
        public IEnumerator MovingAStackMovesTheConnectedLoopStack([Values] TestingMode mode)
        {
            var stackModel0 = GraphModel.CreateStack(string.Empty, new Vector2(-100, -100));
            var loopStack = GraphModel.CreateLoopStack<WhileHeaderModel>(new Vector2(50, 50));
            var whileModel = loopStack.CreateLoopNode(stackModel0, 0);
            GraphModel.CreateEdge(loopStack.InputPort, whileModel.OutputPort);

            yield return TestMove(mode,
                mouseDelta: new Vector2(20, 10),
                movedNodes: new INodeModel[] { stackModel0 },
                expectedMovedDependencies: new INodeModel[] { loopStack }
            );
        }

        [UnityTest]
        public IEnumerator MovingAFloatingNodeMovesConnectedToken([Values] TestingMode mode)
        {
            BinaryOperatorNodeModel operatorModel = GraphModel.CreateBinaryOperatorNode(BinaryOperatorKind.Add, new Vector2(-100, -100));
            IConstantNodeModel intModel = GraphModel.CreateConstantNode("int", typeof(int).GenerateTypeHandle(GraphModel.Stencil), new Vector2(-150, -100));
            GraphModel.CreateEdge(operatorModel.InputPortA, intModel.OutputPort);

            yield return TestMove(mode,
                mouseDelta: new Vector2(20, 10),
                movedNodes: new INodeModel[] { operatorModel },
                expectedMovedDependencies: new INodeModel[] { intModel }
            );
        }

        [UnityTest]
        public IEnumerator MovingAStackMovesStackedNodeConnectedFloatingNode([Values] TestingMode mode)
        {
            var stackModel0 = GraphModel.CreateStack(string.Empty, new Vector2(-100, -100));
            UnaryOperatorNodeModel unary = stackModel0.CreateStackedNode<UnaryOperatorNodeModel>("postDecr", setup: n => n.Kind = UnaryOperatorKind.PostDecrement);
            BinaryOperatorNodeModel operatorModel = GraphModel.CreateBinaryOperatorNode(BinaryOperatorKind.Add, new Vector2(-100, -100));
            IConstantNodeModel intModel = GraphModel.CreateConstantNode("int", typeof(int).GenerateTypeHandle(GraphModel.Stencil), new Vector2(-150, -100));
            GraphModel.CreateEdge(unary.InputPort, operatorModel.OutputPort);
            GraphModel.CreateEdge(operatorModel.InputPortA, intModel.OutputPort);

            yield return TestMove(mode,
                mouseDelta: new Vector2(20, 10),
                movedNodes: new INodeModel[] { stackModel0 },
                expectedMovedDependencies: new INodeModel[] { operatorModel, intModel }
            );
        }

        [UnityTest]
        public IEnumerator MovingAStackMovesConditionStacks([Values] TestingMode mode)
        {
            var stackModel0 = GraphModel.CreateStack(string.Empty, new Vector2(-100, -100));
            IfConditionNodeModel conditionNodeModel = stackModel0.CreateStackedNode<IfConditionNodeModel>("cond");
            var thenStack = GraphModel.CreateStack(string.Empty, new Vector2(100, 100));
            var elseStack = GraphModel.CreateStack(string.Empty, new Vector2(200, 100));
            var joinStack = GraphModel.CreateStack(string.Empty, new Vector2(-100, 200));
            GraphModel.CreateEdge(thenStack.InputPorts.First(), conditionNodeModel.ThenPort);
            GraphModel.CreateEdge(elseStack.InputPorts.First(), conditionNodeModel.ElsePort);
            GraphModel.CreateEdge(joinStack.InputPorts.First(), thenStack.OutputPorts.First());
            GraphModel.CreateEdge(joinStack.InputPorts.First(), elseStack.OutputPorts.First());

            yield return TestMove(mode,
                mouseDelta: new Vector2(20, 10),
                movedNodes: new INodeModel[] { stackModel0 },
                expectedMovedDependencies: new INodeModel[] { thenStack, elseStack, joinStack }
            );
        }

        [UnityTest]
        public IEnumerator MovingThenStackDoesntMoveElseOrConditionStacks([Values] TestingMode mode)
        {
            var stackModel0 = GraphModel.CreateStack(string.Empty, new Vector2(-100, -100));
            IfConditionNodeModel conditionNodeModel = stackModel0.CreateStackedNode<IfConditionNodeModel>("cond");
            var thenStack = GraphModel.CreateStack(string.Empty, new Vector2(100, 100));
            var elseStack = GraphModel.CreateStack(string.Empty, new Vector2(200, 100));
            var joinStack = GraphModel.CreateStack(string.Empty, new Vector2(-100, 200));
            GraphModel.CreateEdge(thenStack.InputPorts.First(), conditionNodeModel.ThenPort);
            GraphModel.CreateEdge(joinStack.InputPorts.First(), thenStack.OutputPorts.First());
            GraphModel.CreateEdge(joinStack.InputPorts.First(), elseStack.OutputPorts.First());

            yield return TestMove(mode,
                mouseDelta: new Vector2(20, 10),
                movedNodes: new INodeModel[] { thenStack },
                expectedMovedDependencies: new INodeModel[] { joinStack },
                expectedUnmovedDependencies: new INodeModel[] { stackModel0, elseStack }
            );
        }

        IEnumerator TestMove(TestingMode mode, Vector2 mouseDelta, INodeModel[] movedNodes,
            INodeModel[] expectedMovedDependencies,
            INodeModel[] expectedUnmovedDependencies = null)
        {
            const float epsilon = 0.00001f;

            Vector2 startMousePos = new Vector2(42, 13);
            List<Vector2> initPositions = expectedMovedDependencies.Select(x => x.Position).ToList();
            List<Vector2> initUnmovedPositions = expectedUnmovedDependencies != null ? expectedUnmovedDependencies.Select(x => x.Position).ToList() : new List<Vector2>();

            yield return TestPrereqActionPostreq(mode,
                () =>
                {
                    for (int i = 0; i < expectedMovedDependencies.Length; i++)
                    {
                        INodeModel model = GraphModel.NodesByGuid[expectedMovedDependencies[i].Guid];
                        GraphElement element = GraphView.UIController.ModelsToNodeMapping[model];

                        Assert.That(model.Position.x, Is.EqualTo(initPositions[i].x).Within(epsilon));
                        Assert.That(model.Position.y, Is.EqualTo(initPositions[i].y).Within(epsilon));
                        Assert.That(element.GetPosition().position.x, Is.EqualTo(initPositions[i].x).Within(epsilon));
                        Assert.That(element.GetPosition().position.y, Is.EqualTo(initPositions[i].y).Within(epsilon));
                    }
                },
                frame =>
                {
                    switch (frame)
                    {
                        case 0:
                            List<ISelectable> selectables = movedNodes.Select(x => GraphView.UIController.ModelsToNodeMapping[x]).Cast<ISelectable>().ToList();
                            GraphView.PositionDependenciesManagers.StartNotifyMove(selectables, startMousePos);
                            GraphView.PositionDependenciesManagers.ProcessMovedNodes(startMousePos + mouseDelta);
                            for (int i = 0; i < expectedMovedDependencies.Length; i++)
                            {
                                INodeModel model = expectedMovedDependencies[i];
                                GraphElement element = GraphView.UIController.ModelsToNodeMapping[model];
                                Assert.That(model.Position.x, Is.EqualTo(initPositions[i].x).Within(epsilon));
                                Assert.That(model.Position.y, Is.EqualTo(initPositions[i].y).Within(epsilon));
                                Assert.That(element.GetPosition().position.x, Is.EqualTo(initPositions[i].x).Within(epsilon));
                                Assert.That(element.GetPosition().position.y, Is.EqualTo(initPositions[i].y).Within(epsilon));
                            }
                            return TestPhase.WaitForNextFrame;
                        default:
                            GraphView.PositionDependenciesManagers.StopNotifyMove();
                            return TestPhase.Done;
                    }
                },
                () =>
                {
                    for (int i = 0; i < expectedMovedDependencies.Length; i++)
                    {
                        INodeModel model = GraphModel.NodesByGuid[expectedMovedDependencies[i].Guid];
                        GraphElement element = GraphView.UIController.ModelsToNodeMapping[model];
                        Assert.That(model.Position.x, Is.EqualTo(initPositions[i].x + mouseDelta.x).Within(epsilon), () => $"Model {model} was expected to have moved");
                        Assert.That(model.Position.y, Is.EqualTo(initPositions[i].y + mouseDelta.y).Within(epsilon), () => $"Model {model} was expected to have moved");
                        Assert.That(element.GetPosition().position.x, Is.EqualTo(initPositions[i].x + mouseDelta.x).Within(epsilon));
                        Assert.That(element.GetPosition().position.y, Is.EqualTo(initPositions[i].y + mouseDelta.y).Within(epsilon));
                    }

                    if (expectedUnmovedDependencies != null)
                    {
                        for (int i = 0; i < expectedUnmovedDependencies.Length; i++)
                        {
                            INodeModel model = GraphModel.NodesByGuid[expectedUnmovedDependencies[i].Guid];
                            GraphElement element = GraphView.UIController.ModelsToNodeMapping[model];
                            Assert.That(model.Position.x, Is.EqualTo(initUnmovedPositions[i].x).Within(epsilon));
                            Assert.That(model.Position.y, Is.EqualTo(initUnmovedPositions[i].y).Within(epsilon));
                            Assert.That(element.GetPosition().position.x, Is.EqualTo(initUnmovedPositions[i].x).Within(epsilon));
                            Assert.That(element.GetPosition().position.y, Is.EqualTo(initUnmovedPositions[i].y).Within(epsilon));
                        }
                    }
                }
            );
        }
    }
}
