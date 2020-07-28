using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor.VisualScripting.Editor;
using UnityEditor.VisualScripting.Editor.SmartSearch;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model;
using UnityEditor.VisualScripting.Model.Stencils;
using UnityEditor.VisualScriptingTests.UI;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Blackboard = UnityEditor.VisualScripting.Editor.Blackboard;

// ReSharper disable AccessToStaticMemberViaDerivedType

namespace UnityEditor.VisualScriptingTests.Actions
{
    [Category("Stack")]
    [Category("Action")]
    class StackActionTests : BaseFixture
    {
        protected override bool CreateGraphOnStartup => true;
        protected override Type CreatedGraphType => typeof(ClassStencil);

        [Test]
        public void Test_CreateStackedNodeFromSearcherAction([Values] TestingMode mode)
        {
            var stack = GraphModel.CreateStack("stack", Vector2.zero);
            var set = stack.CreateSetPropertyGroupNode(-1);
            var db = new GraphElementSearcherDatabase(Stencil).AddUnaryOperators().Build();
            var item = (StackNodeModelSearcherItem)db.Search("post decr", out _)[0];

            TestPrereqActionPostreq(mode,
                () =>
                {
                    RefreshReference(ref stack);
                    RefreshReference(ref set);
                    Assert.That(GetNodeCount(), Is.EqualTo(1));
                    Assert.That(stack.NodeModels.Count, Is.EqualTo(1));
                    return new CreateStackedNodeFromSearcherAction(stack, 1, item);
                },
                () =>
                {
                    RefreshReference(ref stack);
                    RefreshReference(ref set);
                    Assert.That(GetNodeCount(), Is.EqualTo(1));
                    Assert.That(stack.NodeModels.Count, Is.EqualTo(2));

                    var stackedNodes = stack.NodeModels.ToList();
                    Assert.That(stackedNodes[0], Is.TypeOf<SetPropertyGroupNodeModel>());
                    Assert.That(stackedNodes[1], Is.TypeOf<UnaryOperatorNodeModel>());
                }
            );
        }

        static IEnumerable<object[]> GetStackSearcherItemTestCases()
        {
            foreach (TestingMode testingMode in Enum.GetValues(typeof(TestingMode)))
            {
                // Test ForEach node creation
                yield return MakeStackSearcherItemTestCase(
                    testingMode, typeof(ForEachNodeModel), new[] {typeof(ForEachHeaderModel)},
                    (stencil, stack) =>
                    {
                        var db = new GraphElementSearcherDatabase(stencil).AddControlFlows().Build();
                        var items = db.Search("for each", out _);
                        return (StackNodeModelSearcherItem)items[0];
                    });

                // Test If node creation (complete)
                yield return MakeStackSearcherItemTestCase(
                    testingMode, typeof(IfConditionNodeModel),
                    new[] {typeof(StackModel), typeof(StackModel), typeof(StackModel)},
                    (stencil, stack) =>
                    {
                        var db = new GraphElementSearcherDatabase(stencil).AddControlFlows().Build();
                        var items = db.Search("if complete", out _);
                        return (StackNodeModelSearcherItem)items[2];
                    });
            }
        }

        static object[] MakeStackSearcherItemTestCase(TestingMode testingMode, Type expectedNodeType,
            IEnumerable expectedStackTypes, Func<Stencil, StackBaseModel, StackNodeModelSearcherItem> makeNodeFunc)
        {
            return new object[] { testingMode, expectedNodeType, expectedStackTypes, makeNodeFunc };
        }

        [Test, TestCaseSource(nameof(GetStackSearcherItemTestCases))]
        public void Test_CreateStackedNodesFromSearcherAction(TestingMode mode, Type expectedNodeType,
            Type[] expectedStackTypes, Func<Stencil, StackBaseModel, StackNodeModelSearcherItem> makeItemFunc)
        {
            var stack = GraphModel.CreateStack("stack", Vector2.zero);
            var item = makeItemFunc(Stencil, stack);

            TestPrereqActionPostreq(mode,
                () =>
                {
                    RefreshReference(ref stack);

                    Assert.That(GetNodeCount(), Is.EqualTo(1));
                    Assert.That(GetStackCount(), Is.EqualTo(1));
                    Assert.That(stack.NodeModels.Count, Is.EqualTo(0));
                    return new CreateStackedNodeFromSearcherAction(stack, -1, item);
                },
                () =>
                {
                    RefreshReference(ref stack);

                    Assert.That(GetNodeCount(), Is.EqualTo(1 + expectedStackTypes.Length));
                    Assert.That(GetStackCount(), Is.EqualTo(1 + expectedStackTypes.Length));
                    Assert.That(stack.NodeModels.Count, Is.EqualTo(1));

                    var stackedNodes = stack.NodeModels.ToList();
                    Assert.That(stackedNodes[0].GetType(), Is.EqualTo(expectedNodeType));

                    int cnt = 1;
                    foreach (var expectedStackType in expectedStackTypes)
                    {
                        var newStack = GetStack(cnt++);
                        Assert.That(newStack.GetType(), Is.EqualTo(expectedStackType));
                    }
                }
            );
        }

        [Test]
        public void Test_CreateStackAction([Values] TestingMode mode)
        {
            TestPrereqActionPostreq(mode,
                () =>
                {
                    Assert.That(GetStackCount(), Is.EqualTo(0));
                    return new CreateStacksForNodesAction(new List<StackCreationOptions> { new StackCreationOptions(Vector2.zero) });
                },
                () =>
                {
                    Assert.That(GetStackCount(), Is.EqualTo(1));
                });
        }

        [Test]
        public void DuplicateStackedNode([Values] TestingMode mode)
        {
            var stack = GraphModel.CreateStack("stack", Vector2.zero);
            stack.CreateStackedNode<Type0FakeNodeModel>("test");

            TestPrereqActionPostreq(mode,
                () =>
                {
                    stack = GetStack(0);
                    Assert.That(stack.NodeModels.Count, Is.EqualTo(1));
                }, () =>
                {
                    stack = GetStack(0);
                    var node = stack.NodeModels.Single();

                    TargetInsertionInfo info;
                    info.OperationName = "Duplicate";
                    info.Delta = Vector2.one;
                    info.TargetStack = stack;
                    info.TargetStackInsertionIndex = -1;

                    IEditorDataModel editorDataModel = m_Store.GetState().EditorDataModel;
                    VseGraphView.CopyPasteData copyPasteData = VseGraphView.GatherCopiedElementsData(new List<IGraphElementModel> { node });
                    Assert.That(copyPasteData.IsEmpty(), Is.False);

                    return new PasteSerializedDataAction(GraphModel, info, editorDataModel, copyPasteData);
                }, () =>
                {
                    stack = GetStack(0);
                    Assert.That(stack.NodeModels.Count, Is.EqualTo(2));
                });
        }

        [Test]
        public void Test_MoveNodesFromSeparateStacks([Values] TestingMode mode)
        {
            string customPrefix = "InitialStack";
            var stack1 = GraphModel.CreateStack(customPrefix + "1", Vector2.zero);
            var stack2 = GraphModel.CreateStack(customPrefix + "2", Vector2.right * 400f);

            stack1.CreateStackedNode<Type0FakeNodeModel>("A");
            stack1.CreateStackedNode<Type0FakeNodeModel>("B");

            stack2.CreateStackedNode<Type0FakeNodeModel>("C");

            Vector2 newStackOffset = Vector2.down * 500f;

            TestPrereqActionPostreq(mode,
                () =>
                {
                    Assert.That(GetStackCount(), Is.EqualTo(2));
                    Assert.That(GetAllStacks().All(s => s.Title.StartsWith(customPrefix)), Is.True);

                    var stackMovements = GetAllStacks().Select(s => new StackCreationOptions(s.Position + newStackOffset, s.NodeModels.ToList()));

                    return new CreateStacksForNodesAction(stackMovements.ToList());
                },
                () =>
                {
                    Assert.That(GetStackCount(), Is.EqualTo(2));
                    Assert.That(GetAllStacks().Any(s => s.Title.StartsWith(customPrefix)), Is.False);
                });
        }

        [Test]
        public void Test_DeleteStackAction_OneEmptyStack([Values] TestingMode mode)
        {
            GraphModel.CreateStack(string.Empty, Vector2.zero);

            TestPrereqActionPostreq(mode,
                () =>
                {
                    Assert.That(GetStackCount(), Is.EqualTo(1));
                    return new DeleteElementsAction(GetFloatingStack(0));
                },
                () =>
                {
                    Assert.That(GetStackCount(), Is.EqualTo(0));
                });
        }

        [Test]
        public void Test_DeleteElementsAction_OneStackWithNodes([Values] TestingMode mode)
        {
            GraphModel.CreateStack(string.Empty, Vector2.zero);
            var stack = GetStack(0);
            stack.CreateStackedNode<Type0FakeNodeModel>("A");
            stack.CreateStackedNode<Type0FakeNodeModel>("B");

            TestPrereqActionPostreq(mode,
                () =>
                {
                    Assert.That(GetStackCount(), Is.EqualTo(1));
                    Assert.That(GetStack(0).NodeModels.Count, Is.EqualTo(2));
                    return new DeleteElementsAction(GetFloatingStack(0));
                },
                () =>
                {
                    Assert.That(GetStackCount(), Is.EqualTo(0));
                });
        }

        [Test]
        public void Test_ChangeStackedNode([Values] TestingMode mode)
        {
            var decl = GraphModel.CreateGraphVariableDeclaration("a", TypeHandle.Bool, true);
            var stackModel = GraphModel.CreateStack(string.Empty, Vector2.zero);
            var set = stackModel.CreateStackedNode<SetVariableNodeModel>("set");
            var v1 = GraphModel.CreateVariableNode(decl, Vector2.left);
            var v2 = GraphModel.CreateVariableNode(decl, Vector2.left);
            GraphModel.CreateEdge(set.InstancePort, v1.OutputPort);
            GraphModel.CreateEdge(set.ValuePort, v2.OutputPort);

            var db = new GraphElementSearcherDatabase(Stencil).AddNodesWithSearcherItemAttribute().Build();
            var item = (StackNodeModelSearcherItem)db.Search("log", out _)[0];

            TestPrereqActionPostreq(mode,
                () =>
                {
                    RefreshReference(ref stackModel);

                    Assert.That(stackModel.NodeModels.Count, Is.EqualTo(1));
                    Assert.That(stackModel.NodeModels.Single(), Is.TypeOf<SetVariableNodeModel>());
                    set = (SetVariableNodeModel)stackModel.NodeModels.Single();
                    Assert.That(v1.OutputPort, Is.ConnectedTo(set.InstancePort));
                    Assert.That(v2.OutputPort, Is.ConnectedTo(set.ValuePort));
                    return new ChangeStackedNodeAction(set, stackModel, item);
                },
                () =>
                {
                    RefreshReference(ref stackModel);

                    Assert.That(stackModel.NodeModels.Count, Is.EqualTo(1));
                    Assert.That(stackModel.NodeModels.Single(), Is.TypeOf<LogNodeModel>());
                    var log = stackModel.NodeModels.OfType<LogNodeModel>().Single();
                    Assert.That(v1.OutputPort, Is.ConnectedTo(log.InputPort));
                    Assert.That(v2.OutputPort.Connected, Is.False);
                });
        }

        [Test]
        public void Test_ChangeStackedNode_ToSameNodeModel([Values] TestingMode mode)
        {
            var stackModel = GraphModel.CreateStack(string.Empty, Vector2.zero);

            var nodeA = stackModel.CreateStackedNode<Type0FakeNodeModel>("A");
            var unaryNode = stackModel.CreateUnaryStatementNode(UnaryOperatorKind.PostDecrement, 1);
            var nodeB = stackModel.CreateStackedNode<Type0FakeNodeModel>("B");
            var decl = GraphModel.CreateGraphVariableDeclaration("v", TypeHandle.Float, true);
            var variableNode = GraphModel.CreateVariableNode(decl, Vector2.zero);

            GraphModel.CreateEdge(unaryNode.InputPort, variableNode.OutputPort);

            var db = new GraphElementSearcherDatabase(Stencil).AddUnaryOperators().Build();
            var item = (StackNodeModelSearcherItem)db.Search("post incr", out _)[0];

            TestPrereqActionPostreq(mode,
                () =>
                {
                    Assert.That(stackModel.NodeModels.Count, Is.EqualTo(3));
                    Assert.That(nodeA, Has.IndexInStack(0, stackModel));
                    var oldNode = GetStackedNode(0, 1) as UnaryOperatorNodeModel;
                    Assert.NotNull(oldNode);
                    Assert.That(nodeB, Has.IndexInStack(2, stackModel));
                    Assert.That(variableNode.OutputPort, Is.ConnectedTo(oldNode.InputPort));
                    return new ChangeStackedNodeAction(oldNode, stackModel, item);
                },
                () =>
                {
                    Assert.That(stackModel.NodeModels.Count, Is.EqualTo(3));
                    Assert.That(nodeA, Has.IndexInStack(0, stackModel));
                    Assert.That(unaryNode.IsStacked, Is.False);
                    Assert.That(nodeB, Has.IndexInStack(2, stackModel));

                    var newNode = GetStackedNode(0, 1);
                    Assert.That(newNode, Is.Not.Null);
                    Assert.That(newNode, Is.InstanceOf<UnaryOperatorNodeModel>());

                    var newUnaryNode = (UnaryOperatorNodeModel)newNode;
                    Assert.That(newUnaryNode.Kind, Is.EqualTo(UnaryOperatorKind.PostIncrement));
                    Assert.That(variableNode.OutputPort, Is.ConnectedTo(newUnaryNode.InputPort));
                });
        }

        [Test]
        public void Test_ChangeStackedNode_ToDifferentNodeModel([Values] TestingMode mode)
        {
            var stackModel = GraphModel.CreateStack(string.Empty, Vector2.zero);
            var nodeA = stackModel.CreateStackedNode<Type0FakeNodeModel>("A");
            var unaryNode = stackModel.CreateUnaryStatementNode(UnaryOperatorKind.PostDecrement, 1);
            var nodeB = stackModel.CreateStackedNode<Type0FakeNodeModel>("B");
            var decl = GraphModel.CreateGraphVariableDeclaration("v", TypeHandle.Float, true);
            var variableNode = GraphModel.CreateVariableNode(decl, Vector2.zero);

            GraphModel.CreateEdge(unaryNode.InputPort, variableNode.OutputPort);

            var db = new GraphElementSearcherDatabase(Stencil).AddControlFlows().Build();
            var item = (StackNodeModelSearcherItem)db.Search("while", out _)[0];

            TestPrereqActionPostreq(mode,
                () =>
                {
                    RefreshReference(ref stackModel);
                    RefreshReference(ref nodeA);
                    RefreshReference(ref unaryNode);
                    RefreshReference(ref nodeB);
                    RefreshReference(ref variableNode);

                    Assert.That(stackModel.NodeModels.Count, Is.EqualTo(3));
                    Assert.That(nodeA, Has.IndexInStack(0, stackModel));

                    var unary = stackModel.NodeModels.ElementAt(1) as UnaryOperatorNodeModel;
                    Assert.That(unaryNode, Is.Not.Null);
                    Assert.That(nodeB, Has.IndexInStack(2, stackModel));
                    Assert.NotNull(unary);
                    Assert.That(variableNode.OutputPort, Is.ConnectedTo(unary.InputPort));
                    return new ChangeStackedNodeAction(unary, stackModel, item);
                },
                () =>
                {
                    RefreshReference(ref stackModel);
                    RefreshReference(ref nodeA);
                    RefreshReference(ref unaryNode);
                    RefreshReference(ref nodeB);
                    RefreshReference(ref variableNode);

                    Assert.That(stackModel.NodeModels.Count, Is.EqualTo(3));
                    Assert.That(nodeA, Has.IndexInStack(0, stackModel));
                    Assert.That(unaryNode, Is.Not.InsideStack(stackModel));
                    Assert.That(nodeB, Has.IndexInStack(2, stackModel));

                    var newNode = GetStackedNode(0, 1);
                    Assert.That(newNode, Is.Not.Null);
                    Assert.That(newNode, Is.InstanceOf<LoopNodeModel>());
                });
        }

        [Test]
        public void Test_MoveStackedNodeAction_ToSameStack([Values] TestingMode mode)
        {
            var stack = GraphModel.CreateStack(string.Empty, Vector2.zero);
            var nodeA = stack.CreateStackedNode<Type0FakeNodeModel>("A");
            var nodeB = stack.CreateStackedNode<Type0FakeNodeModel>("B");
            var nodeC = stack.CreateStackedNode<Type0FakeNodeModel>("C");
            var nodeD = stack.CreateStackedNode<Type0FakeNodeModel>("D");

            TestPrereqActionPostreq(mode,
                () =>
                {
                    RefreshReference(ref stack);
                    RefreshReference(ref nodeA);
                    RefreshReference(ref nodeB);
                    RefreshReference(ref nodeC);
                    RefreshReference(ref nodeD);
                    Assert.That(stack.NodeModels.Count, Is.EqualTo(4));
                    Assert.That(nodeA, Has.IndexInStack(0, stack));
                    Assert.That(nodeB, Has.IndexInStack(1, stack));
                    Assert.That(nodeC, Has.IndexInStack(2, stack));
                    Assert.That(nodeD, Has.IndexInStack(3, stack));
                    return new MoveStackedNodesAction(new[] {nodeA}, stack, 2);
                },
                () =>
                {
                    RefreshReference(ref stack);
                    RefreshReference(ref nodeA);
                    RefreshReference(ref nodeB);
                    RefreshReference(ref nodeC);
                    RefreshReference(ref nodeD);
                    Assert.That(stack.NodeModels.Count, Is.EqualTo(4));
                    Assert.That(nodeB, Has.IndexInStack(0, stack));
                    Assert.That(nodeC, Has.IndexInStack(1, stack));
                    Assert.That(nodeA, Has.IndexInStack(2, stack));
                    Assert.That(nodeD, Has.IndexInStack(3, stack));
                });

            TestPrereqActionPostreq(mode,
                () =>
                {
                    RefreshReference(ref stack);
                    RefreshReference(ref nodeA);
                    RefreshReference(ref nodeB);
                    RefreshReference(ref nodeC);
                    RefreshReference(ref nodeD);
                    Assert.That(stack.NodeModels.Count, Is.EqualTo(4));
                    Assert.That(nodeB, Has.IndexInStack(0, stack));
                    Assert.That(nodeC, Has.IndexInStack(1, stack));
                    Assert.That(nodeA, Has.IndexInStack(2, stack));
                    Assert.That(nodeD, Has.IndexInStack(3, stack));
                    return new MoveStackedNodesAction(new[] { nodeD }, stack, 0);
                },
                () =>
                {
                    RefreshReference(ref stack);
                    RefreshReference(ref nodeA);
                    RefreshReference(ref nodeB);
                    RefreshReference(ref nodeC);
                    RefreshReference(ref nodeD);
                    Assert.That(stack.NodeModels.Count, Is.EqualTo(4));
                    Assert.That(nodeD, Has.IndexInStack(0, stack));
                    Assert.That(nodeB, Has.IndexInStack(1, stack));
                    Assert.That(nodeC, Has.IndexInStack(2, stack));
                    Assert.That(nodeA, Has.IndexInStack(3, stack));
                });
        }

        [Test]
        public void Test_MoveMultipleStackedNodesAction_ToSameStack([Values] TestingMode mode)
        {
            var stack = GraphModel.CreateStack(string.Empty, Vector2.zero);
            var nodeA = stack.CreateStackedNode<Type0FakeNodeModel>("A");
            var nodeB = stack.CreateStackedNode<Type0FakeNodeModel>("B");
            var nodeC = stack.CreateStackedNode<Type0FakeNodeModel>("C");
            var nodeD = stack.CreateStackedNode<Type0FakeNodeModel>("D");

            TestPrereqActionPostreq(mode,
                () =>
                {
                    RefreshReference(ref stack);
                    RefreshReference(ref nodeA);
                    RefreshReference(ref nodeB);
                    RefreshReference(ref nodeC);
                    RefreshReference(ref nodeD);

                    Assert.That(stack.NodeModels.Count, Is.EqualTo(4));
                    Assert.That(nodeA, Has.IndexInStack(0, stack));
                    Assert.That(nodeB, Has.IndexInStack(1, stack));
                    Assert.That(nodeC, Has.IndexInStack(2, stack));
                    Assert.That(nodeD, Has.IndexInStack(3, stack));
                    return new MoveStackedNodesAction(new[] {nodeA, nodeC}, stack, 2);
                },
                () =>
                {
                    RefreshReference(ref stack);
                    RefreshReference(ref nodeA);
                    RefreshReference(ref nodeB);
                    RefreshReference(ref nodeC);
                    RefreshReference(ref nodeD);

                    Assert.That(stack.NodeModels.Count, Is.EqualTo(4));
                    Assert.That(nodeB, Has.IndexInStack(0, stack));
                    Assert.That(nodeD, Has.IndexInStack(1, stack));
                    Assert.That(nodeA, Has.IndexInStack(2, stack));
                    Assert.That(nodeC, Has.IndexInStack(3, stack));
                });

            TestPrereqActionPostreq(mode,
                () =>
                {
                    RefreshReference(ref stack);
                    RefreshReference(ref nodeA);
                    RefreshReference(ref nodeB);
                    RefreshReference(ref nodeC);
                    RefreshReference(ref nodeD);

                    Assert.That(stack.NodeModels.Count, Is.EqualTo(4));
                    Assert.That(nodeB, Has.IndexInStack(0, stack));
                    Assert.That(nodeD, Has.IndexInStack(1, stack));
                    Assert.That(nodeA, Has.IndexInStack(2, stack));
                    Assert.That(nodeC, Has.IndexInStack(3, stack));
                    return new MoveStackedNodesAction(new[] {nodeD, nodeC}, stack, 0);
                },
                () =>
                {
                    RefreshReference(ref stack);
                    RefreshReference(ref nodeA);
                    RefreshReference(ref nodeB);
                    RefreshReference(ref nodeC);
                    RefreshReference(ref nodeD);

                    Assert.That(stack.NodeModels.Count, Is.EqualTo(4));
                    Assert.That(nodeD, Has.IndexInStack(0, stack));
                    Assert.That(nodeC, Has.IndexInStack(1, stack));
                    Assert.That(nodeB, Has.IndexInStack(2, stack));
                    Assert.That(nodeA, Has.IndexInStack(3, stack));
                });

            TestPrereqActionPostreq(mode,
                () =>
                {
                    RefreshReference(ref stack);
                    RefreshReference(ref nodeA);
                    RefreshReference(ref nodeB);
                    RefreshReference(ref nodeC);
                    RefreshReference(ref nodeD);

                    Assert.That(stack.NodeModels.Count, Is.EqualTo(4));
                    Assert.That(nodeD, Has.IndexInStack(0, stack));
                    Assert.That(nodeC, Has.IndexInStack(1, stack));
                    Assert.That(nodeB, Has.IndexInStack(2, stack));
                    Assert.That(nodeA, Has.IndexInStack(3, stack));
                    return new MoveStackedNodesAction(new[] {nodeA, nodeB, nodeC, nodeD}, stack, 0);
                },
                () =>
                {
                    RefreshReference(ref stack);
                    RefreshReference(ref nodeA);
                    RefreshReference(ref nodeB);
                    RefreshReference(ref nodeC);
                    RefreshReference(ref nodeD);

                    Assert.That(stack.NodeModels.Count, Is.EqualTo(4));
                    Assert.That(nodeA, Has.IndexInStack(0, stack));
                    Assert.That(nodeB, Has.IndexInStack(1, stack));
                    Assert.That(nodeC, Has.IndexInStack(2, stack));
                    Assert.That(nodeD, Has.IndexInStack(3, stack));
                });
        }

        [Test]
        public void Test_MoveStackedNodeAction_ToDifferentStack([Values] TestingMode mode)
        {
            var stack0 = GraphModel.CreateStack(string.Empty, Vector2.zero);
            var nodeA = stack0.CreateStackedNode<Type0FakeNodeModel>("A");
            var nodeB = stack0.CreateStackedNode<Type0FakeNodeModel>("B");
            var nodeC = stack0.CreateStackedNode<Type0FakeNodeModel>("C");
            var stack1 = GraphModel.CreateStack(string.Empty, Vector2.zero);
            var nodeD = stack1.CreateStackedNode<Type0FakeNodeModel>("D");
            var nodeE = stack1.CreateStackedNode<Type0FakeNodeModel>("E");
            var nodeF = stack1.CreateStackedNode<Type0FakeNodeModel>("F");

            TestPrereqActionPostreq(mode,
                () =>
                {
                    RefreshReference(ref stack0);
                    RefreshReference(ref nodeA);
                    RefreshReference(ref nodeB);
                    RefreshReference(ref nodeC);
                    RefreshReference(ref stack1);
                    RefreshReference(ref nodeD);
                    RefreshReference(ref nodeE);
                    RefreshReference(ref nodeF);

                    Assert.That(stack0.NodeModels.Count, Is.EqualTo(3));
                    Assert.That(nodeA, Has.IndexInStack(0, stack0));
                    Assert.That(nodeB, Has.IndexInStack(1, stack0));
                    Assert.That(nodeC, Has.IndexInStack(2, stack0));
                    Assert.That(stack1.NodeModels.Count, Is.EqualTo(3));
                    Assert.That(nodeD, Has.IndexInStack(0, stack1));
                    Assert.That(nodeE, Has.IndexInStack(1, stack1));
                    Assert.That(nodeF, Has.IndexInStack(2, stack1));
                    return new MoveStackedNodesAction(new[] {nodeA}, stack1, 1);
                },
                () =>
                {
                    RefreshReference(ref stack0);
                    RefreshReference(ref nodeA);
                    RefreshReference(ref nodeB);
                    RefreshReference(ref nodeC);
                    RefreshReference(ref stack1);
                    RefreshReference(ref nodeD);
                    RefreshReference(ref nodeE);
                    RefreshReference(ref nodeF);

                    Assert.That(stack0.NodeModels.Count, Is.EqualTo(2));
                    Assert.That(nodeB, Has.IndexInStack(0, stack0));
                    Assert.That(nodeC, Has.IndexInStack(1, stack0));
                    Assert.That(stack1.NodeModels.Count, Is.EqualTo(4));
                    Assert.That(nodeD, Has.IndexInStack(0, stack1));
                    Assert.That(nodeA, Has.IndexInStack(1, stack1));
                    Assert.That(nodeE, Has.IndexInStack(2, stack1));
                    Assert.That(nodeF, Has.IndexInStack(3, stack1));
                });

            TestPrereqActionPostreq(mode,
                () =>
                {
                    RefreshReference(ref stack0);
                    RefreshReference(ref nodeA);
                    RefreshReference(ref nodeB);
                    RefreshReference(ref nodeC);
                    RefreshReference(ref stack1);
                    RefreshReference(ref nodeD);
                    RefreshReference(ref nodeE);
                    RefreshReference(ref nodeF);

                    Assert.That(stack0.NodeModels.Count, Is.EqualTo(2));
                    Assert.That(nodeB, Has.IndexInStack(0, stack0));
                    Assert.That(nodeC, Has.IndexInStack(1, stack0));
                    Assert.That(stack1.NodeModels.Count, Is.EqualTo(4));
                    Assert.That(nodeD, Has.IndexInStack(0, stack1));
                    Assert.That(nodeA, Has.IndexInStack(1, stack1));
                    Assert.That(nodeE, Has.IndexInStack(2, stack1));
                    Assert.That(nodeF, Has.IndexInStack(3, stack1));
                    return new MoveStackedNodesAction(new[] {nodeF}, stack0, 0);
                },
                () =>
                {
                    RefreshReference(ref stack0);
                    RefreshReference(ref nodeA);
                    RefreshReference(ref nodeB);
                    RefreshReference(ref nodeC);
                    RefreshReference(ref stack1);
                    RefreshReference(ref nodeD);
                    RefreshReference(ref nodeE);
                    RefreshReference(ref nodeF);

                    Assert.That(stack0.NodeModels.Count, Is.EqualTo(3));
                    Assert.That(nodeF, Has.IndexInStack(0, stack0));
                    Assert.That(nodeB, Has.IndexInStack(1, stack0));
                    Assert.That(nodeC, Has.IndexInStack(2, stack0));
                    Assert.That(stack1.NodeModels.Count, Is.EqualTo(3));
                    Assert.That(nodeD, Has.IndexInStack(0, stack1));
                    Assert.That(nodeA, Has.IndexInStack(1, stack1));
                    Assert.That(nodeE, Has.IndexInStack(2, stack1));
                });
        }

        [Test]
        public void Test_MoveMultipleStackedNodesAction_ToDifferentStack([Values] TestingMode mode)
        {
            var stack0 = GraphModel.CreateStack(string.Empty, Vector2.zero);
            var nodeA = stack0.CreateStackedNode<Type0FakeNodeModel>("A");
            var nodeB = stack0.CreateStackedNode<Type0FakeNodeModel>("B");
            var nodeC = stack0.CreateStackedNode<Type0FakeNodeModel>("C");
            var stack1 = GraphModel.CreateStack(string.Empty, Vector2.zero);
            var nodeD = stack1.CreateStackedNode<Type0FakeNodeModel>("D");
            var nodeE = stack1.CreateStackedNode<Type0FakeNodeModel>("E");
            var nodeF = stack1.CreateStackedNode<Type0FakeNodeModel>("F");

            TestPrereqActionPostreq(mode,
                () =>
                {
                    RefreshReference(ref stack0);
                    RefreshReference(ref nodeA);
                    RefreshReference(ref nodeB);
                    RefreshReference(ref nodeC);
                    RefreshReference(ref stack1);
                    RefreshReference(ref nodeD);
                    RefreshReference(ref nodeE);
                    RefreshReference(ref nodeF);

                    Assert.That(stack0.NodeModels.Count, Is.EqualTo(3));
                    Assert.That(nodeA, Has.IndexInStack(0, stack0));
                    Assert.That(nodeB, Has.IndexInStack(1, stack0));
                    Assert.That(nodeC, Has.IndexInStack(2, stack0));
                    Assert.That(stack1.NodeModels.Count, Is.EqualTo(3));
                    Assert.That(nodeD, Has.IndexInStack(0, stack1));
                    Assert.That(nodeE, Has.IndexInStack(1, stack1));
                    Assert.That(nodeF, Has.IndexInStack(2, stack1));
                    return new MoveStackedNodesAction(new[] {nodeA, nodeC}, stack1, 1);
                },
                () =>
                {
                    RefreshReference(ref stack0);
                    RefreshReference(ref nodeA);
                    RefreshReference(ref nodeB);
                    RefreshReference(ref nodeC);
                    RefreshReference(ref stack1);
                    RefreshReference(ref nodeD);
                    RefreshReference(ref nodeE);
                    RefreshReference(ref nodeF);

                    Assert.That(stack0.NodeModels.Count, Is.EqualTo(1));
                    Assert.That(nodeB, Has.IndexInStack(0, stack0));
                    Assert.That(stack1.NodeModels.Count, Is.EqualTo(5));
                    Assert.That(nodeD, Has.IndexInStack(0, stack1));
                    Assert.That(nodeA, Has.IndexInStack(1, stack1));
                    Assert.That(nodeC, Has.IndexInStack(2, stack1));
                    Assert.That(nodeE, Has.IndexInStack(3, stack1));
                    Assert.That(nodeF, Has.IndexInStack(4, stack1));
                });

            TestPrereqActionPostreq(mode,
                () =>
                {
                    RefreshReference(ref stack0);
                    RefreshReference(ref nodeA);
                    RefreshReference(ref nodeB);
                    RefreshReference(ref nodeC);
                    RefreshReference(ref stack1);
                    RefreshReference(ref nodeD);
                    RefreshReference(ref nodeE);
                    RefreshReference(ref nodeF);

                    Assert.That(stack0.NodeModels.Count, Is.EqualTo(1));
                    Assert.That(nodeB, Has.IndexInStack(0, stack0));
                    Assert.That(stack1.NodeModels.Count, Is.EqualTo(5));
                    Assert.That(nodeD, Has.IndexInStack(0, stack1));
                    Assert.That(nodeA, Has.IndexInStack(1, stack1));
                    Assert.That(nodeC, Has.IndexInStack(2, stack1));
                    Assert.That(nodeE, Has.IndexInStack(3, stack1));
                    Assert.That(nodeF, Has.IndexInStack(4, stack1));
                    return new MoveStackedNodesAction(new[] {nodeF, nodeD}, stack0, 0);
                },
                () =>
                {
                    RefreshReference(ref stack0);
                    RefreshReference(ref nodeA);
                    RefreshReference(ref nodeB);
                    RefreshReference(ref nodeC);
                    RefreshReference(ref stack1);
                    RefreshReference(ref nodeD);
                    RefreshReference(ref nodeE);
                    RefreshReference(ref nodeF);

                    Assert.That(stack0.NodeModels.Count, Is.EqualTo(3));
                    Assert.That(nodeF, Has.IndexInStack(0, stack0));
                    Assert.That(nodeD, Has.IndexInStack(1, stack0));
                    Assert.That(nodeB, Has.IndexInStack(2, stack0));
                    Assert.That(stack1.NodeModels.Count, Is.EqualTo(3));
                    Assert.That(nodeA, Has.IndexInStack(0, stack1));
                    Assert.That(nodeC, Has.IndexInStack(1, stack1));
                    Assert.That(nodeE, Has.IndexInStack(2, stack1));
                });

            TestPrereqActionPostreq(mode,
                () =>
                {
                    RefreshReference(ref stack0);
                    RefreshReference(ref nodeA);
                    RefreshReference(ref nodeB);
                    RefreshReference(ref nodeC);
                    RefreshReference(ref stack1);
                    RefreshReference(ref nodeD);
                    RefreshReference(ref nodeE);
                    RefreshReference(ref nodeF);

                    Assert.That(stack0.NodeModels.Count, Is.EqualTo(3));
                    Assert.That(nodeF, Has.IndexInStack(0, stack0));
                    Assert.That(nodeD, Has.IndexInStack(1, stack0));
                    Assert.That(nodeB, Has.IndexInStack(2, stack0));
                    Assert.That(stack1.NodeModels.Count, Is.EqualTo(3));
                    Assert.That(nodeA, Has.IndexInStack(0, stack1));
                    Assert.That(nodeC, Has.IndexInStack(1, stack1));
                    Assert.That(nodeE, Has.IndexInStack(2, stack1));
                    return new MoveStackedNodesAction(new[] {nodeF, nodeC}, stack1, 2);
                },
                () =>
                {
                    RefreshReference(ref stack0);
                    RefreshReference(ref nodeA);
                    RefreshReference(ref nodeB);
                    RefreshReference(ref nodeC);
                    RefreshReference(ref stack1);
                    RefreshReference(ref nodeD);
                    RefreshReference(ref nodeE);
                    RefreshReference(ref nodeF);

                    Assert.That(stack0.NodeModels.Count, Is.EqualTo(2));
                    Assert.That(nodeD, Has.IndexInStack(0, stack0));
                    Assert.That(nodeB, Has.IndexInStack(1, stack0));
                    Assert.That(stack1.NodeModels.Count, Is.EqualTo(4));
                    Assert.That(nodeA, Has.IndexInStack(0, stack1));
                    Assert.That(nodeE, Has.IndexInStack(1, stack1));
                    Assert.That(nodeF, Has.IndexInStack(2, stack1));
                    Assert.That(nodeC, Has.IndexInStack(3, stack1));
                });
        }

        [Test]
        public void Test_SplitOneStack([Values] TestingMode mode)
        {
            var stack0 = GraphModel.CreateStack("stack0", Vector2.zero);
            var nodeA = stack0.CreateStackedNode<Type0FakeNodeModel>("A");
            var nodeB = stack0.CreateStackedNode<Type0FakeNodeModel>("B");

//            GraphModel.CreateEdge(stack0.InputPorts[0], stack0.OutputPorts.First());
//            GraphModel.CreateEdge(stack2.InputPorts[0], stack0.OutputPorts.First());

            TestPrereqActionPostreq(mode,
                () =>
                {
                    RefreshReference(ref stack0);
                    RefreshReference(ref nodeA);
                    RefreshReference(ref nodeB);
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(GetStackCount(), Is.EqualTo(1));
                    Assert.That(nodeA, Is.InsideStack(stack0));
                    Assert.That(nodeB, Is.InsideStack(stack0));
                    Assert.That(stack0.NodeModels.Count, Is.EqualTo(2));
                    return new SplitStackAction(stack0, 1);
                },
                () =>
                {
                    RefreshReference(ref stack0);
                    RefreshReference(ref nodeA);
                    RefreshReference(ref nodeB);
                    var stack3 = GetStack(1);
                    Assert.That(GetEdgeCount(), Is.EqualTo(1));
                    Assert.That(GetStackCount(), Is.EqualTo(2));
                    Assert.That(stack0, Is.ConnectedToStack(stack3));
                    Assert.That(nodeA, Is.InsideStack(stack0));
                    Assert.That(nodeB, Is.InsideStack(stack3));
                    Assert.That(stack0.NodeModels.Count, Is.EqualTo(1));
                    Assert.That(stack3.NodeModels.Count, Is.EqualTo(1));
                });
        }

        [Test]
        public void Test_AddIfToConnectedStackTransfersConnection([Values] TestingMode mode)
        {
            var db = new GraphElementSearcherDatabase(Stencil).AddControlFlows().Build();
            var item = (StackNodeModelSearcherItem)db.Search("if", out _)[0];
            {
                var s1 = GraphModel.CreateStack("stack1", Vector2.up);
                var s2 = GraphModel.CreateStack("stack2", Vector2.zero);
                var s3 = GraphModel.CreateStack("stack3", Vector2.down);
                GraphModel.CreateEdge(s2.InputPorts[0], s1.OutputPorts[0]);
                GraphModel.CreateEdge(s3.InputPorts[0], s2.OutputPorts[0]);
            }
            TestPrereqActionPostreq(mode,
                () =>
                {
                    Assert.That(GetEdgeCount(), Is.EqualTo(2));
                    Assert.That(GetStackCount(), Is.EqualTo(3));
                    var s1 = GetStack(0);
                    var s2 = GetStack(1);
                    var s3 = GetStack(2);
                    Assert.That(s1.OutputPorts[0], Is.ConnectedTo(s2.InputPorts[0]));
                    Assert.That(s2.OutputPorts[0], Is.ConnectedTo(s3.InputPorts[0]));
                    return new CreateStackedNodeFromSearcherAction(s2, 1, item);
                },
                () =>
                {
                    Assert.That(GetEdgeCount(), Is.EqualTo(2));
                    Assert.That(GetStackCount(), Is.EqualTo(3));
                    var s1 = GetStack(0);
                    var s2 = GetStack(1);
                    var s3 = GetStack(2);
                    var stackedNode = GetStackedNode(1, 0);
                    Assert.That(s1.OutputPorts[0], Is.ConnectedTo(s2.InputPorts[0]));
                    Assert.That(s2.OutputPorts[0], Is.ConnectedTo(s3.InputPorts[0]));
                    Assert.That(stackedNode, Is.TypeOf<IfConditionNodeModel>());
                    var ifNode = (IfConditionNodeModel)stackedNode;
                    Assert.That(ifNode.ThenPort, Is.ConnectedTo(s3.InputPorts[0]));
                });
        }

        [Test]
        public void Test_AddIfToConnectedStackTransfersConnection2([Values] TestingMode mode)
        {
            {
                var s1 = GraphModel.CreateStack("stack1", Vector2.up);
                var s2 = GraphModel.CreateStack("stack2", Vector2.zero);
                GraphModel.CreateEdge(s2.InputPorts[0], s1.OutputPorts[0]);

                var sIf = GraphModel.CreateStack("stackIf", Vector2.down);
                var sThen = GraphModel.CreateStack("stackThen", Vector2.down);
                var sElse = GraphModel.CreateStack("stackElse", Vector2.down);
                sIf.CreateStackedNode<IfConditionNodeModel>();
                GraphModel.CreateEdge(sThen.InputPorts[0], sIf.OutputPorts[0]);
                GraphModel.CreateEdge(sElse.InputPorts[0], sIf.OutputPorts[1]);
            }
            TestPrereqActionPostreq(mode,
                () =>
                {
                    Assert.That(GetEdgeCount(), Is.EqualTo(3));
                    Assert.That(GetStackCount(), Is.EqualTo(5));
                    var s1 = GetStack(0);
                    var s2 = GetStack(1);
                    var sIf = GetStack(2);
                    var sThen = GetStack(3);
                    var sElse = GetStack(4);
                    Assert.That(s1.OutputPorts.Single().ConnectionPortModels.Count(), Is.EqualTo(1));

                    var ifNode = sIf.NodeModels.OfType<IfConditionNodeModel>().Single();
                    Assert.That(s1.OutputPorts[0], Is.ConnectedTo(s2.InputPorts[0]));
                    Assert.That(sIf.OutputPorts[0], Is.ConnectedTo(sThen.InputPorts[0]));
                    Assert.That(sIf.OutputPorts[1], Is.ConnectedTo(sElse.InputPorts[0]));
                    Assert.That(ifNode.ThenPort, Is.ConnectedTo(sThen.InputPorts[0]));
                    Assert.That(ifNode.ElsePort, Is.ConnectedTo(sElse.InputPorts[0]));
                    return new MoveStackedNodesAction(new[] {ifNode}, s1, 0);
                },
                () =>
                {
                    Assert.That(GetEdgeCount(), Is.EqualTo(2));
                    Assert.That(GetStackCount(), Is.EqualTo(4));
                    var s1 = GetStack(0);
                    var s2 = GetStack(1);
                    var sThen = GetStack(2);
                    var sElse = GetStack(3);
                    var stackedNode = s1.NodeModels.OfType<IfConditionNodeModel>().Single();
                    var ifNode = (IfConditionNodeModel)stackedNode;
                    Assert.That(s1.OutputPorts[0], Is.Not.ConnectedTo(s2.InputPorts[0]));
                    Assert.That(s1.OutputPorts[0], Is.ConnectedTo(sThen.InputPorts[0]));
                    Assert.That(s1.OutputPorts[1], Is.ConnectedTo(sElse.InputPorts[0]));
                    Assert.That(ifNode.ThenPort, Is.ConnectedTo(sThen.InputPorts[0]));
                    Assert.That(ifNode.ElsePort, Is.ConnectedTo(sElse.InputPorts[0]));
                });
        }

        [Test]
        public void Test_DeleteIfToConnectedStackTransfersConnection([Values] TestingMode mode)
        {
            {
                var s1 = GraphModel.CreateStack("stack1", Vector2.up);
                var s2 = GraphModel.CreateStack("stack2", Vector2.zero);
                var s3 = GraphModel.CreateStack("stack3", Vector2.down);
                var ifNode = s2.CreateStackedNode<IfConditionNodeModel>("if");
                GraphModel.CreateEdge(s2.InputPorts[0], s1.OutputPorts[0]);
                GraphModel.CreateEdge(s3.InputPorts[0], ifNode.ThenPort);
            }
            TestPrereqActionPostreq(mode,
                () =>
                {
                    Assert.That(GetEdgeCount(), Is.EqualTo(2));
                    Assert.That(GetStackCount(), Is.EqualTo(3));
                    var s1 = GetStack(0);
                    var s2 = GetStack(1);
                    var s3 = GetStack(2);
                    var stackedNode = GetStackedNode(1, 0);
                    Assert.That(stackedNode, Is.TypeOf<IfConditionNodeModel>());
                    var ifNode = (IfConditionNodeModel)stackedNode;
                    Assert.That(s1.OutputPorts[0], Is.ConnectedTo(s2.InputPorts[0]));
                    Assert.That(s2.OutputPorts[0], Is.ConnectedTo(s3.InputPorts[0]));
                    Assert.That(ifNode.ThenPort, Is.ConnectedTo(s3.InputPorts[0]));
                    return new DeleteElementsAction(ifNode);
                },
                () =>
                {
                    Assert.That(GetEdgeCount(), Is.EqualTo(2));
                    Assert.That(GetStackCount(), Is.EqualTo(3));
                    var s1 = GetStack(0);
                    var s2 = GetStack(1);
                    var s3 = GetStack(2);
                    Assert.That(s1.OutputPorts[0], Is.ConnectedTo(s2.InputPorts[0]));
                    Assert.That(s2.OutputPorts[0], Is.ConnectedTo(s3.InputPorts[0]));
                });
        }

        [Test]
        public void Test_MoveIfToNewStackPreservesConnections([Values] TestingMode mode)
        {
            {
                var ifStack = GraphModel.CreateStack("stack1", Vector2.up);
                var thenStack = GraphModel.CreateStack("thenStack", Vector2.left);
                var elseStack = GraphModel.CreateStack("elseStack", Vector2.right);
                var logNode = ifStack.CreateStackedNode<LogNodeModel>("log");
                var ifNode = ifStack.CreateStackedNode<IfConditionNodeModel>("if");
                GraphModel.CreateEdge(thenStack.InputPorts[0], ifNode.ThenPort);
                GraphModel.CreateEdge(elseStack.InputPorts[0], ifNode.ElsePort);
            }
            TestPrereqActionPostreq(mode,
                () =>
                {
                    Assert.That(GetEdgeCount(), Is.EqualTo(2));
                    Assert.That(GetStackCount(), Is.EqualTo(3));
                    var ifStack = GetStack(0);
                    var thenStack = GetStack(1);
                    var elseStack = GetStack(2);
                    var logNode = ifStack.NodeModels[0];
                    var ifStackNode = ifStack.NodeModels[1];
                    Assert.That(ifStackNode, NUnit.Framework.Is.TypeOf<IfConditionNodeModel>());
                    var ifNode = (IfConditionNodeModel)ifStackNode;
                    Assert.That(ifStack.OutputPorts[0], Is.ConnectedTo(thenStack.InputPorts[0]));
                    Assert.That(ifStack.OutputPorts[1], Is.ConnectedTo(elseStack.InputPorts[0]));
                    Assert.That(ifNode.ThenPort, Is.ConnectedTo(thenStack.InputPorts[0]));
                    Assert.That(ifNode.ElsePort, Is.ConnectedTo(elseStack.InputPorts[0]));
                    return new CreateStacksForNodesAction(new List<StackCreationOptions> { new StackCreationOptions(Vector2.zero, new List<INodeModel> {ifNode}) });
                },
                () =>
                {
                    Assert.That(GetEdgeCount(), Is.EqualTo(2));
                    Assert.That(GetStackCount(), Is.EqualTo(4));
                    var oldStack = GetStack(0);
                    var thenStack = GetStack(1);
                    var elseStack = GetStack(2);
                    var newStack = GetStack(3);
                    var logNode = oldStack.NodeModels[0];
                    var ifStackNode = newStack.NodeModels[0];
                    Assert.That(ifStackNode, Is.TypeOf<IfConditionNodeModel>());
                    var ifNode = (IfConditionNodeModel)ifStackNode;
                    Assert.That(ifNode.ThenPort, Is.ConnectedTo(thenStack.InputPorts[0]));
                    Assert.That(ifNode.ElsePort, Is.ConnectedTo(elseStack.InputPorts[0]));
                });
        }

        [Test]
        public void Test_SplitStackAction([Values] TestingMode mode)
        {
            var stack0 = GraphModel.CreateStack("stack0", Vector2.zero);
            var stack1 = GraphModel.CreateStack("stack1", Vector2.zero);
            var stack2 = GraphModel.CreateStack("stack2", Vector2.zero);
            var nodeA = stack1.CreateStackedNode<Type0FakeNodeModel>("A");
            var nodeB = stack1.CreateStackedNode<Type0FakeNodeModel>("B");
            var nodeC = stack1.CreateStackedNode<Type0FakeNodeModel>("C");

            GraphModel.CreateEdge(stack1.InputPorts[0], stack0.OutputPorts.First());
            GraphModel.CreateEdge(stack2.InputPorts[0], stack1.OutputPorts.First());

            TestPrereqActionPostreq(mode,
                () =>
                {
                    stack0 = GetStack(0);
                    stack1 = GetStack(1);
                    stack2 = GetStack(2);
                    Assert.That(GetEdgeCount(), Is.EqualTo(2));
                    Assert.That(GetStackCount(), Is.EqualTo(3));
                    Assert.That(stack0, Is.ConnectedToStack(stack1));
                    Assert.That(stack1, Is.ConnectedToStack(stack2));
                    Assert.That(nodeA, Is.InsideStack(stack1));
                    Assert.That(nodeB, Is.InsideStack(stack1));
                    Assert.That(nodeC, Is.InsideStack(stack1));
                    Assert.That(stack0.NodeModels.Count, Is.EqualTo(0));
                    Assert.That(stack1.NodeModels.Count, Is.EqualTo(3));
                    Assert.That(stack2.NodeModels.Count, Is.EqualTo(0));
                    return new SplitStackAction(stack1, 1);
                },
                () =>
                {
                    stack0 = GetStack(0);
                    stack1 = GetStack(1);
                    stack2 = GetStack(2);
                    var stack3 = GetStack(3);
                    Assert.That(GetEdgeCount(), Is.EqualTo(3));
                    Assert.That(GetStackCount(), Is.EqualTo(4));
                    Assert.That(stack0, Is.ConnectedToStack(stack1));
                    Assert.That(stack1, Is.ConnectedToStack(stack3));
                    Assert.That(stack3, Is.ConnectedToStack(stack2));
                    Assert.That(nodeA, Is.InsideStack(stack1));
                    Assert.That(nodeB, Is.InsideStack(stack3));
                    Assert.That(nodeC, Is.InsideStack(stack3));
                    Assert.That(stack0.NodeModels.Count, Is.EqualTo(0));
                    Assert.That(stack1.NodeModels.Count, Is.EqualTo(1));
                    Assert.That(stack2.NodeModels.Count, Is.EqualTo(0));
                    Assert.That(stack3.NodeModels.Count, Is.EqualTo(2));
                });
        }

        [Test]
        public void Test_MergeStackAction([Values] TestingMode mode)
        {
            GraphModel.CreateStack("stack0", Vector2.zero);
            GraphModel.CreateStack("stack1", Vector2.zero);
            GraphModel.CreateStack("stack2", Vector2.zero);
            var stack0 = GetStack(0);
            var stack1 = GetStack(1);
            var nodeA = stack0.CreateStackedNode<Type0FakeNodeModel>("A");
            var nodeB = stack0.CreateStackedNode<Type0FakeNodeModel>("B");
            var nodeC = stack1.CreateStackedNode<Type0FakeNodeModel>("C");

            GraphModel.CreateEdge(stack1.InputPorts[0], stack0.OutputPorts.First());
            GraphModel.CreateEdge(GetStack(2).InputPorts[0], stack1.OutputPorts.First());

            TestPrereqActionPostreq(mode,
                () =>
                {
                    stack0 = GetStack(0);
                    stack1 = GetStack(1);
                    Assert.That(GetEdgeCount(), Is.EqualTo(2));
                    Assert.That(GetStackCount(), Is.EqualTo(3));
                    Assert.That(stack0, Is.ConnectedToStack(stack1));
                    Assert.That(stack1, Is.ConnectedToStack(GetStack(2)));
                    Assert.That(nodeA, Is.InsideStack(stack0));
                    Assert.That(nodeB, Is.InsideStack(stack0));
                    Assert.That(nodeC, Is.InsideStack(stack1));
                    Assert.That(stack0.NodeModels.Count, Is.EqualTo(2));
                    Assert.That(stack1.NodeModels.Count, Is.EqualTo(1));
                    Assert.That(GetStack(2).NodeModels.Count, Is.EqualTo(0));
                    return new MergeStackAction(stack0, stack1);
                },
                () =>
                {
                    stack0 = GetStack(0);
                    stack1 = GetStack(1);
                    Assert.That(GetEdgeCount(), Is.EqualTo(1));
                    Assert.That(GetStackCount(), Is.EqualTo(2));
                    Assert.That(stack0, Is.ConnectedToStack(stack1));
                    Assert.That(nodeA, Is.InsideStack(stack0));
                    Assert.That(nodeB, Is.InsideStack(stack0));
                    Assert.That(nodeC, Is.InsideStack(stack0));
                    Assert.That(stack0.NodeModels.Count, Is.EqualTo(3));
                    Assert.That(stack1.NodeModels.Count, Is.EqualTo(0));
                });
        }
    }

    class StackActionUITests : BaseUIFixture
    {
        protected override bool CreateGraphOnStartup => true;
        protected override Type CreatedGraphType => typeof(ClassStencil);

        [UnityTest]
        public IEnumerator Test_DuplicateCommandSkipsSingleInstanceModels()
        {
            GraphModel.CreateStack("stack", new Vector2(200, 50));
            Vector2 position = new Vector2(200, 150);
            GraphModel.CreateNode<UniqueInstanceFunctionModel>("Start", position);

            Store.Dispatch(new RefreshUIAction(UpdateFlags.All));
            yield return null;

            Assert.That(GetGraphElements().Count, Is.EqualTo(2));

            Helpers.MouseClickEvent(GetGraphElement(0).GetGlobalCenter()); // Ensure we have focus
            yield return null;

            GraphView.selection.Add(GetGraphElement(0));
            GraphView.selection.Add(GetGraphElement(1));

            Helpers.ExecuteCommand("Duplicate");
            yield return null;

            Store.Dispatch(new RefreshUIAction(UpdateFlags.All));
            yield return null;

            Assert.That(GetGraphElements().Count, Is.EqualTo(3), "Duplicate should have skipped the Start event function node");
        }

        [UnityTest]
        public IEnumerator Test_BlackboardLocalScopeShowsAllVariablesFromSelection()
        {
            Stencil stencil = GraphModel.Stencil;

            var functionModel = GraphModel.CreateFunction("function", new Vector2(200, 50));
            var functionVariable  = functionModel.CreateFunctionVariableDeclaration("l", typeof(int).GenerateTypeHandle(stencil));
            var functionParameter = functionModel.CreateAndRegisterFunctionParameterDeclaration("a", typeof(int).GenerateTypeHandle(stencil));

            var stackModel1 = GraphModel.CreateStack("stack1", new Vector2(200, 200));
            var stack1Input = stackModel1.InputPorts[0];
            var stack1Output = stackModel1.OutputPorts[0];

            var stackModel2 = GraphModel.CreateStack("stack2", new Vector2(200, 400));
            var stack2Input = stackModel2.InputPorts[0];

            Store.Dispatch(new RefreshUIAction(UpdateFlags.All));
            yield return null;

            Store.Dispatch(new CreateEdgeAction(stack1Input, functionModel.OutputPort));
            yield return null;

            Store.Dispatch(new CreateEdgeAction(stack2Input, stack1Output));
            yield return null;

            GraphView.selection.Add(GetGraphElement(5));
            Store.Dispatch(new RefreshUIAction(UpdateFlags.All));
            yield return null;

            Blackboard blackboard = GraphView.UIController.Blackboard;
            var allVariables = GraphView.UIController.GetAllVariableDeclarationsFromSelection(GraphView.selection).ToList();

            Assert.That(allVariables.Find(x => (VariableDeclarationModel)x.Item1 == functionVariable) != null);
            Assert.That(allVariables.Find(x => (VariableDeclarationModel)x.Item1 == functionParameter) != null);

            // Test assumes that section displays
            var blackboardVariableFields = blackboard.Query<BlackboardVariableField>().ToList();

            Assert.That(blackboardVariableFields.Find(x => (VariableDeclarationModel)x.VariableDeclarationModel == functionVariable) != null);
            Assert.That(blackboardVariableFields.Find(x => (VariableDeclarationModel)x.VariableDeclarationModel == functionParameter) != null);
        }
    }
}
