using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.GraphElements;
using UnityEditor.GraphToolsFoundation.Overdrive.TestBridge;
using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting;
using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.GraphViewModel;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

// ReSharper disable AccessToStaticMemberViaDerivedType

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.UI
{
    class MoveDependencyTests : BaseUIFixture
    {
        protected override bool CreateGraphOnStartup => true;
        protected override Type CreatedGraphType => typeof(ClassStencil);

        [Test]
        public void DeleteNodeDoesRemoveTheDependency()
        {
            var mgr = new PositionDependenciesManager(GraphView, GraphView.window.Preferences);
            var operatorModel = GraphModel.CreateNode<Type0FakeNodeModel>("Node0", new Vector2(-100, -100));
            IConstantNodeModel intModel = GraphModel.CreateConstantNode("int", typeof(int).GenerateTypeHandle(GraphModel.Stencil), new Vector2(-150, -100));
            var edge = GraphModel.CreateEdge(operatorModel.Input0, intModel.OutputPort);
            mgr.AddPositionDependency(edge);
            mgr.Remove(operatorModel.Guid, intModel.Guid);
            Assert.That(mgr.GetDependencies(operatorModel), Is.Null);
        }

        [UnityTest, Ignore("@theor needs to figure this one out")]
        public IEnumerator EndToEndMoveDependencyWithPanning()
        {
            var node0 = GraphModel.CreateNode<Type0FakeNodeModel>(string.Empty, new Vector2(100, -100));
            var node1 = GraphModel.CreateNode<Type0FakeNodeModel>(string.Empty, new Vector2(100, 100));
            GraphModel.CreateEdge(node1.Input0, node0.Output0);

            Store.Dispatch(new RefreshUIAction(UpdateFlags.All));
            yield return null;
            GraphView.FrameAll();
            yield return null;

            bool needsMouseUp = false;
            try
            {
                using (var scheduler = GraphView.CreateTimerEventSchedulerWrapper())
                {
                    GraphElement stackNode = GraphView.UIController.ModelsToNodeMapping[node0];
                    Vector2 startPos = stackNode.GetPosition().position;
                    Vector2 otherStartPos = node1.Position;
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
                    Assert.That(node1.Position, Is.EqualTo(otherStartPos + delta));
                }
            }
            finally
            {
                if (needsMouseUp)
                    Helpers.MouseUpEvent(Vector2.zero);
            }
        }

        [UnityTest]
        public IEnumerator MovingAFloatingNodeMovesConnectedToken([Values] TestingMode mode)
        {
            var operatorModel = GraphModel.CreateNode<Type0FakeNodeModel>("Node0", new Vector2(-100, -100));
            IConstantNodeModel intModel = GraphModel.CreateConstantNode("int", typeof(int).GenerateTypeHandle(GraphModel.Stencil), new Vector2(-150, -100));
            GraphModel.CreateEdge(operatorModel.Input0, intModel.OutputPort);

            yield return TestMove(mode,
                mouseDelta: new Vector2(20, 10),
                movedNodes: new INodeModel[] { operatorModel },
                expectedMovedDependencies: new INodeModel[] { intModel }
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
                            List<ISelectableGraphElement> selectables = movedNodes.Select(x => GraphView.UIController.ModelsToNodeMapping[x]).Cast<ISelectableGraphElement>().ToList();
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
