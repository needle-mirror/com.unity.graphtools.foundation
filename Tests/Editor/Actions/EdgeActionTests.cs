using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using NUnit.Framework;
using UnityEditor.VisualScripting.Editor;
using UnityEditor.VisualScripting.Editor.SmartSearch;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model;
using UnityEditor.VisualScripting.Model.Stencils;
using UnityEngine;
using static UnityEditor.VisualScripting.Model.VSPreferences;

// ReSharper disable AccessToStaticMemberViaDerivedType

namespace UnityEditor.VisualScriptingTests.Actions
{
    [Category("Edge")]
    [Category("Action")]
    class EdgeActionTests : BaseFixture
    {
        protected override bool CreateGraphOnStartup => true;
        protected override Type CreatedGraphType => typeof(ClassStencil);

        [Test]
        public void Test_CreateEdgeAction_OneEdge([Values] TestingMode mode)
        {
            var node0 = GraphModel.CreateNode<Type0FakeNodeModel>("Node0", new Vector2(-200, 0));
            var node1 = GraphModel.CreateNode<Type0FakeNodeModel>("Node1", new Vector2(200, 0));

            TestPrereqActionPostreq(mode,
                () =>
                {
                    RefreshReference(ref node0);
                    RefreshReference(ref node1);
                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(node0.Input0, Is.Not.ConnectedTo(node1.Output0));
                    return new CreateEdgeAction(node0.Input0, node1.Output0);
                },
                () =>
                {
                    RefreshReference(ref node0);
                    RefreshReference(ref node1);
                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GetEdgeCount(), Is.EqualTo(1));
                    Assert.That(node0.Input0, Is.ConnectedTo(node1.Output0));
                });
        }

        // no undo as it doesn't do anything
        [Test]
        public void Test_CreateEdgeAction_Duplicate([Values(TestingMode.Action)] TestingMode mode)
        {
            var node0 = GraphModel.CreateNode<Type0FakeNodeModel>("Node0", new Vector2(-200, 0));
            var node1 = GraphModel.CreateNode<Type0FakeNodeModel>("Node1", new Vector2(200, 0));

            GraphModel.CreateEdge(node0.Input0, node1.Output0);

            TestPrereqActionPostreq(mode,
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GetEdgeCount(), Is.EqualTo(1));
                    Assert.That(node0.Input0, Is.ConnectedTo(node1.Output0));
                    return new CreateEdgeAction(node0.Input0, node1.Output0);
                },
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GetEdgeCount(), Is.EqualTo(1));
                    Assert.That(node0.Input0, Is.ConnectedTo(node1.Output0));
                });
        }

        [PublicAPI]
        public enum ItemizeTestType
        {
            Enabled, Disabled
        }

        static IEnumerable<object[]> GetItemizeTestCases()
        {
            foreach (TestingMode testingMode in Enum.GetValues(typeof(TestingMode)))
            {
                // test both itemize option and non ItemizeTestType option
                foreach (ItemizeTestType itemizeTest in Enum.GetValues(typeof(ItemizeTestType)))
                {
                    yield return MakeItemizeTestCase(testingMode, ItemizeOptions.Variables, itemizeTest,
                        graphModel =>
                        {
                            string name = graphModel.GetUniqueName("myInt");
                            VariableDeclarationModel decl = graphModel.CreateGraphVariableDeclaration(name, typeof(int).GenerateTypeHandle(graphModel.Stencil), true);
                            return graphModel.CreateVariableNode(decl, Vector2.zero);
                        }
                    );

                    yield return MakeItemizeTestCase(testingMode, ItemizeOptions.SystemConstants, itemizeTest,
                        graphModel =>
                        {
                            void PreDefineSetup(SystemConstantNodeModel m)
                            {
                                m.ReturnType = typeof(float).GenerateTypeHandle(graphModel.Stencil);
                                m.DeclaringType = typeof(Mathf).GenerateTypeHandle(graphModel.Stencil);
                                m.Identifier = "PI";
                            }

                            return graphModel.CreateNode<SystemConstantNodeModel>("Constant", Vector2.zero, SpawnFlags.Default, PreDefineSetup);
                        });

                    yield return MakeItemizeTestCase(testingMode, ItemizeOptions.Constants, itemizeTest,
                        graphModel =>
                        {
                            string name = graphModel.GetUniqueName("myInt");
                            return graphModel.CreateConstantNode(name, typeof(int).GenerateTypeHandle(graphModel.Stencil), Vector2.zero);
                        });
                }
            }
        }

        static object[] MakeItemizeTestCase(TestingMode testingMode, ItemizeOptions options, ItemizeTestType itemizeTest, Func<VSGraphModel, IHasMainOutputPort> makeNode)
        {
            return new object[] { testingMode, options, itemizeTest, makeNode };
        }

        [Test, TestCaseSource(nameof(GetItemizeTestCases))]
        public void Test_CreateEdgeAction_Itemize(TestingMode testingMode, ItemizeOptions options, ItemizeTestType itemizeTest, Func<VSGraphModel, IHasMainOutputPort> makeNode)
        {
            // save initial itemize options
            VSPreferences pref = ((TestState)m_Store.GetState()).Preferences;
            ItemizeOptions initialOptions = pref.CurrentItemizeOptions;

            try
            {
                // create int node
                IHasMainOutputPort node0 = makeNode(GraphModel);

                // create Addition node
                BinaryOperatorNodeModel opNode = GraphModel.CreateBinaryOperatorNode(BinaryOperatorKind.Add, Vector2.zero);

                // enable Itemize depending on the test case
                var itemizeOptions = ItemizeOptions.Nothing;
                pref.CurrentItemizeOptions = (itemizeTest == ItemizeTestType.Enabled) ? options : itemizeOptions;

                // connect int to first input
                m_Store.Dispatch(new CreateEdgeAction(opNode.InputPortA, node0.OutputPort));
                m_Store.Update();

                // test how the node reacts to getting connected a second time
                TestPrereqActionPostreq(testingMode,
                    () =>
                    {
                        RefreshReference(ref node0);
                        RefreshReference(ref opNode);
                        var binOp = GraphModel.GetAllNodes().OfType<BinaryOperatorNodeModel>().First();
                        IPortModel input0 = binOp.InputPortA;
                        IPortModel input1 = binOp.InputPortB;
                        IPortModel binOutput = binOp.OutputPort;
                        Assert.That(GetNodeCount(), Is.EqualTo(2));
                        Assert.That(GetEdgeCount(), Is.EqualTo(1));
                        Assert.That(input0, Is.ConnectedTo(node0.OutputPort));
                        Assert.That(input1, Is.Not.ConnectedTo(node0.OutputPort));
                        Assert.That(binOutput.Connected, Is.False);
                        return new CreateEdgeAction(input1, node0.OutputPort);
                    },
                    () =>
                    {
                        RefreshReference(ref node0);
                        RefreshReference(ref opNode);
                        var binOp = GraphModel.GetAllNodes().OfType<BinaryOperatorNodeModel>().First();
                        IPortModel input0 = binOp.InputPortA;
                        IPortModel input1 = binOp.InputPortB;
                        IPortModel binOutput = binOp.OutputPort;
                        Assert.That(GetEdgeCount(), Is.EqualTo(2));
                        Assert.That(input0, Is.ConnectedTo(node0.OutputPort));
                        Assert.That(binOutput.Connected, Is.False);

                        if (itemizeTest == ItemizeTestType.Enabled)
                        {
                            Assert.That(GetNodeCount(), Is.EqualTo(3));
                            IHasMainOutputPort newNode = GetNode(2) as IHasMainOutputPort;
                            Assert.NotNull(newNode);
                            Assert.That(newNode, Is.TypeOf(node0.GetType()));
                            IPortModel output1 = newNode.OutputPort;
                            Assert.That(input1, Is.ConnectedTo(output1));
                        }
                        else
                        {
                            Assert.That(GetNodeCount(), Is.EqualTo(2));
                        }
                    });
            }
            finally
            {
                // restore itemize options
                pref.CurrentItemizeOptions = initialOptions;
            }
        }

        // TODO: Test itemization when connecting to stacked nodes (both grouped and ungrouped)

        [Test]
        public void Test_CreateEdgeAction_ManyEdge([Values] TestingMode mode)
        {
            var node0 = GraphModel.CreateNode<Type0FakeNodeModel>("Node0", new Vector2(-200, 0));
            var node1 = GraphModel.CreateNode<Type0FakeNodeModel>("Node1", new Vector2(200, 0));

            TestPrereqActionPostreq(mode,
                () =>
                {
                    RefreshReference(ref node0);
                    RefreshReference(ref node1);
                    var input0 = node0.Input0;
                    var input1 = node0.Input1;
                    var input2 = node0.Input2;
                    var output0 = node1.Output0;
                    var output1 = node1.Output1;
                    var output2 = node1.Output2;
                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(input0, Is.Not.ConnectedTo(output0));
                    Assert.That(input1, Is.Not.ConnectedTo(output1));
                    Assert.That(input2, Is.Not.ConnectedTo(output2));
                    return new CreateEdgeAction(input0, output0);
                },
                () =>
                {
                    RefreshReference(ref node0);
                    RefreshReference(ref node1);
                    var input0 = node0.Input0;
                    var input1 = node0.Input1;
                    var input2 = node0.Input2;
                    var output0 = node1.Output0;
                    var output1 = node1.Output1;
                    var output2 = node1.Output2;
                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GetEdgeCount(), Is.EqualTo(1));
                    Assert.That(input0, Is.ConnectedTo(output0));
                    Assert.That(input1, Is.Not.ConnectedTo(output1));
                    Assert.That(input2, Is.Not.ConnectedTo(output2));
                });

            TestPrereqActionPostreq(mode,
                () =>
                {
                    RefreshReference(ref node0);
                    RefreshReference(ref node1);
                    var input0 = node0.Input0;
                    var input1 = node0.Input1;
                    var input2 = node0.Input2;
                    var output0 = node1.Output0;
                    var output1 = node1.Output1;
                    var output2 = node1.Output2;
                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GetEdgeCount(), Is.EqualTo(1));
                    Assert.That(input0, Is.ConnectedTo(output0));
                    Assert.That(input1, Is.Not.ConnectedTo(output1));
                    Assert.That(input2, Is.Not.ConnectedTo(output2));
                    return new CreateEdgeAction(input1, output1);
                },
                () =>
                {
                    RefreshReference(ref node0);
                    RefreshReference(ref node1);
                    var input0 = node0.Input0;
                    var input1 = node0.Input1;
                    var input2 = node0.Input2;
                    var output0 = node1.Output0;
                    var output1 = node1.Output1;
                    var output2 = node1.Output2;
                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GetEdgeCount(), Is.EqualTo(2));
                    Assert.That(input0, Is.ConnectedTo(output0));
                    Assert.That(input1, Is.ConnectedTo(output1));
                    Assert.That(input2, Is.Not.ConnectedTo(output2));
                });

            TestPrereqActionPostreq(mode,
                () =>
                {
                    RefreshReference(ref node0);
                    RefreshReference(ref node1);
                    var input0 = node0.Input0;
                    var input1 = node0.Input1;
                    var input2 = node0.Input2;
                    var output0 = node1.Output0;
                    var output1 = node1.Output1;
                    var output2 = node1.Output2;
                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GetEdgeCount(), Is.EqualTo(2));
                    Assert.That(input0, Is.ConnectedTo(output0));
                    Assert.That(input1, Is.ConnectedTo(output1));
                    Assert.That(input2, Is.Not.ConnectedTo(output2));
                    return new CreateEdgeAction(input2, output2);
                },
                () =>
                {
                    RefreshReference(ref node0);
                    RefreshReference(ref node1);
                    var input0 = node0.Input0;
                    var input1 = node0.Input1;
                    var input2 = node0.Input2;
                    var output0 = node1.Output0;
                    var output1 = node1.Output1;
                    var output2 = node1.Output2;
                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GetEdgeCount(), Is.EqualTo(3));
                    Assert.That(input0, Is.ConnectedTo(output0));
                    Assert.That(input1, Is.ConnectedTo(output1));
                    Assert.That(input2, Is.ConnectedTo(output2));
                });
        }

        static IEnumerable<object[]> GetCreateTestCases()
        {
            foreach (TestingMode testingMode in Enum.GetValues(typeof(TestingMode)))
            {
                yield return new object[] { testingMode };
            }
        }

        [Test]
        public void Test_CreateNodeFromOutputPort_NoNodeCreated([Values] TestingMode mode)
        {
            var stack1 = GraphModel.CreateStack("Stack1", Vector2.zero);
            var stack2 = GraphModel.CreateStack("Stack2", Vector2.zero);
            var edge = GraphModel.CreateEdge(stack2.InputPorts[0], stack1.OutputPorts[0]);

            TestPrereqActionPostreq(mode,
                () =>
                {
                    Assert.That(GetEdgeCount(), Is.EqualTo(1));
                    return new CreateNodeFromOutputPortAction(
                        stack1.OutputPorts[0],
                        Vector2.down,
                        new GraphNodeModelSearcherItem(new NodeSearcherItemData(typeof(int)), data => null, ""),
                        new List<IEdgeModel> { edge });
                },
                () =>
                {
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                });
        }

        [Test, TestCaseSource(nameof(GetCreateTestCases))]
        public void Test_CreateNodeFromOutputPort_NoConnection(TestingMode testingMode)
        {
            var db = new GraphElementSearcherDatabase(Stencil)
                .AddMethods(typeof(Vector2).GetMethods(BindingFlags.Static | BindingFlags.Public))
                .Build();
            var item = (GraphNodeModelSearcherItem)db.Search("distance", out _).First();

            var node0 = GraphModel.CreateNode<Type1FakeNodeModel>("Node0", Vector2.zero);
            var output0 = node0.Output;

            TestPrereqActionPostreq(testingMode,
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(1));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    return new CreateNodeFromOutputPortAction(output0, Vector2.down, item);
                },
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));

                    var newNode = GetNode(1);
                    Assert.That(newNode, Is.TypeOf<FunctionCallNodeModel>());

                    var portModel = node0.OutputsByDisplayOrder.First();
                    Assert.That(portModel.ConnectionPortModels.Count(), Is.EqualTo(0));
                });
        }

        [Test, TestCaseSource(nameof(GetCreateTestCases))]
        public void Test_CreateNodeFromOutputPort(TestingMode testingMode)
        {
            var db = new GraphElementSearcherDatabase(Stencil).AddBinaryOperators().Build();
            var item = (GraphNodeModelSearcherItem)db.Search("add", out _).First();

            var node0 = GraphModel.CreateNode<Type3FakeNodeModel>("Node0", Vector2.zero);

            TestPrereqActionPostreq(testingMode,
                () =>
                {
                    RefreshReference(ref node0);
                    var output0 = node0.Output;
                    Assert.That(GetNodeCount(), Is.EqualTo(1));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    return new CreateNodeFromOutputPortAction(output0, Vector2.down, item);
                },
                () =>
                {
                    RefreshReference(ref node0);
                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GetEdgeCount(), Is.EqualTo(1));

                    var newNode = GetNode(1);
                    Assert.That(newNode, Is.TypeOf<BinaryOperatorNodeModel>());

                    var newEdge = GetEdge(0);
                    Assert.That(newEdge.InputPortModel.DataType, Is.EqualTo(newEdge.OutputPortModel.DataType));

                    var portModel = node0.Output;
                    Assert.That(portModel.ConnectionPortModels.Single(), Is.EqualTo(newNode.InputsByDisplayOrder.First()));
                });
        }

        [Test, TestCaseSource(nameof(GetCreateTestCases))]
        public void Test_CreateNodeFromExecutionPort_FromInput(TestingMode testingMode)
        {
            var stack = GraphModel.CreateStack("Stack", Vector2.zero);

            TestPrereqActionPostreq(testingMode,
                () =>
                {
                    RefreshReference(ref stack);
                    var input0 = stack.InputPorts[0];
                    Assert.That(GetStackCount(), Is.EqualTo(1));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    return new CreateNodeFromExecutionPortAction(input0, Vector2.down);
                },
                () =>
                {
                    RefreshReference(ref stack);
                    var input0 = stack.InputPorts[0];
                    Assert.That(GetStackCount(), Is.EqualTo(2));
                    Assert.That(GetEdgeCount(), Is.EqualTo(1));
                    var newStack = GetStack(1);
                    Assert.That(newStack, Is.TypeOf<StackModel>());
                    Assert.That(newStack.OutputPorts[0].ConnectionPortModels.First(),
                        Is.EqualTo(stack.InputPorts[0]));
                });
        }

        [Test, TestCaseSource(nameof(GetCreateTestCases))]
        public void Test_CreateNodeFromExecutionPort_FromOutput(TestingMode testingMode)
        {
            var stack = GraphModel.CreateStack("Stack", Vector2.zero);

            TestPrereqActionPostreq(testingMode,
                () =>
                {
                    RefreshReference(ref stack);
                    var output0 = stack.OutputPorts[0];
                    Assert.That(GetStackCount(), Is.EqualTo(1));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    return new CreateNodeFromExecutionPortAction(output0, Vector2.down);
                },
                () =>
                {
                    RefreshReference(ref stack);
                    Assert.That(GetStackCount(), Is.EqualTo(2));
                    Assert.That(GetEdgeCount(), Is.EqualTo(1));

                    var newStack = GetStack(1);
                    Assert.That(newStack, Is.TypeOf<StackModel>());
                    Assert.That(stack.OutputPorts[0].ConnectionPortModels.First(),
                        Is.EqualTo(newStack.InputPorts[0]));
                });
        }

        [Test]
        public void Test_CreateStackedNodeFromOutputPort_NoNodeCreated([Values] TestingMode mode)
        {
            var stack1 = GraphModel.CreateStack("Stack1", Vector2.zero);
            var stack2 = GraphModel.CreateStack("Stack2", Vector2.zero);
            var edge = GraphModel.CreateEdge(stack1.InputPorts[0], stack2.OutputPorts[0]);

            TestPrereqActionPostreq(mode,
                () =>
                {
                    Assert.That(GetEdgeCount(), Is.EqualTo(1));
                    return new CreateStackedNodeFromOutputPortAction(
                        stack1.OutputPorts[0],
                        stack1,
                        0,
                        new StackNodeModelSearcherItem(
                            new NodeSearcherItemData(typeof(int)),
                            data => Enumerable.Empty<IGraphElementModel>().ToArray(),
                            ""),
                        new List<IEdgeModel> { edge });
                },
                () =>
                {
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                });
        }

        [Test]
        public void Test_CreateStackedNodeFromOutputPort([Values] TestingMode mode)
        {
            var db = new GraphElementSearcherDatabase(Stencil).AddNodesWithSearcherItemAttribute().Build();
            var item = (StackNodeModelSearcherItem)db.Search("set", out _).First();

            var stack = GraphModel.CreateStack(string.Empty, Vector2.zero);
            var decl = GraphModel.CreateGraphVariableDeclaration("x", typeof(float).GenerateTypeHandle(Stencil), false);
            GraphModel.CreateVariableNode(decl, Vector2.left * 200);

            TestPrereqActionPostreq(mode,
                () =>
                {
                    var stackNode = GraphModel.StackModels.Single();
                    var otherNode = GraphModel.NodeModels.OfType<IVariableModel>().Single();
                    var portModel = otherNode.OutputPort;

                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(stackNode.NodeModels.Count, Is.EqualTo(0));
                    Assert.That(portModel.Connected, Is.False);
                    return new CreateStackedNodeFromOutputPortAction(portModel, stack, -1, item);
                },
                () =>
                {
                    var stackNode = GraphModel.StackModels.Single();
                    var otherNode = GraphModel.NodeModels.OfType<IVariableModel>().Single();
                    var portModel = otherNode.OutputPort;

                    Assert.That(GetEdgeCount(), Is.EqualTo(1));
                    Assert.That(portModel.Connected, Is.True);

                    var propertyNode = stackNode.NodeModels.OfType<SetPropertyGroupNodeModel>().Single();
                    var propertyPortModel = propertyNode.InstancePort;
                    Assert.That(propertyNode, Is.TypeOf<SetPropertyGroupNodeModel>());
                    Assert.That(propertyPortModel.Connected, Is.True);
                    Assert.That(portModel.ConnectionPortModels.Count(), Is.EqualTo(1));
                    Assert.That(portModel.ConnectionPortModels.Single(), Is.EqualTo(propertyPortModel));
                });
        }

        [Test]
        public void Test_CreateInsertLoopNode([Values] TestingMode mode)
        {
            GraphModel.CreateStack(string.Empty, Vector2.zero);
            GraphModel.CreateLoopStack<ForEachHeaderModel>(Vector2.right * 100);

            TestPrereqActionPostreq(mode,
                () =>
                {
                    var loopStack = GetStack(1) as LoopStackModel;
                    var stack = GetStack(0);
                    Assert.That(stack.NodeModels.Count, Is.EqualTo(0));
                    var portModel = loopStack.InputPort;
                    Assert.That(portModel.Connected, Is.False);
                    return new CreateInsertLoopNodeAction(
                        loopStack.InputPorts.First(), stack, -1, (LoopStackModel)portModel.NodeModel);
                },
                () =>
                {
                    var loopStack = GetStack(1) as LoopStackModel;
                    var stack = GetStack(0);
                    Assert.That(stack.NodeModels.Count, Is.EqualTo(1));
                    var loopNode = stack.NodeModels.First() as ForEachNodeModel;
                    Assert.That(loopNode, Is.Not.Null);
                    Assert.NotNull(loopNode);
                    var portModel = loopNode.OutputPort;

                    Assert.That(portModel.Connected, Is.True);
                    Assert.That(portModel.ConnectionPortModels.Single(), Is.EqualTo(loopStack.InputPort));
                });
        }

        [Test, TestCaseSource(nameof(GetCreateTestCases))]
        public void Test_CreateNodeFromLoopPort_CreateLoopStack(TestingMode testingMode)
        {
            var stack = GraphModel.CreateStack(string.Empty, Vector2.zero);
            var loopNode = stack.CreateStackedNode<WhileNodeModel>("loop");
            var stackCount = GetStackCount();

            TestPrereqActionPostreq(testingMode,
                () =>
                {
                    Assert.That(GetStackCount(), Is.EqualTo(stackCount));
                    var portModel = loopNode.OutputPort;
                    Assert.That(portModel.Connected, Is.False);
                    return new CreateNodeFromLoopPortAction(loopNode.OutputPort, Vector2.zero);
                },
                () =>
                {
                    Assert.That(GetStackCount(), Is.EqualTo(stackCount + 1));
                    var portModel = loopNode.OutputPort;
                    Assert.That(portModel.Connected, Is.True);
                    var connectedStack = portModel.ConnectionPortModels.Single().NodeModel;
                    Assert.That(connectedStack, Is.TypeOf<WhileHeaderModel>());
                });
        }

        [Test]
        public void Test_CreateNodeFromLoopPort_CreateStack([Values] TestingMode mode)
        {
            var stack = GraphModel.CreateStack(string.Empty, Vector2.zero);
            var stackCount = GetStackCount();

            TestPrereqActionPostreq(mode,
                () =>
                {
                    Assert.That(GetStackCount(), Is.EqualTo(stackCount));
                    var portModel = stack.OutputPorts.First();
                    Assert.That(portModel.Connected, Is.False);
                    return new CreateNodeFromLoopPortAction(stack.OutputPorts.First(), Vector2.zero);
                },
                () =>
                {
                    Assert.That(GetStackCount(), Is.EqualTo(stackCount + 1));
                    var portModel = stack.OutputPorts.First();
                    Assert.That(portModel.Connected, Is.True);
                    var connectedStack = portModel.ConnectionPortModels.Single().NodeModel;
                    Assert.That(connectedStack, Is.TypeOf<StackModel>());
                });
        }

        [Test]
        public void Test_DeleteElementsAction_OneEdge([Values] TestingMode mode)
        {
            var node0 = GraphModel.CreateNode<Type0FakeNodeModel>("Node0", new Vector2(-200, 0));
            var node1 = GraphModel.CreateNode<Type0FakeNodeModel>("Node1", new Vector2(200, 0));
            var input0 = node0.Input0;
            var input1 = node0.Input1;
            var input2 = node0.Input2;
            var output0 = node1.Output0;
            var output1 = node1.Output1;
            var output2 = node1.Output2;
            var edge0 = GraphModel.CreateEdge(input0, output0);
            GraphModel.CreateEdge(input1, output1);
            GraphModel.CreateEdge(input2, output2);

            TestPrereqActionPostreq(mode,
                () =>
                {
                    RefreshReference(ref node0);
                    RefreshReference(ref node1);
                    edge0 = GetEdge(0);
                    input0 = node0.Input0;
                    input1 = node0.Input1;
                    input2 = node0.Input2;
                    output0 = node1.Output0;
                    output1 = node1.Output1;
                    output2 = node1.Output2;
                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GetEdgeCount(), Is.EqualTo(3));
                    Assert.That(input0, Is.ConnectedTo(output0));
                    Assert.That(input1, Is.ConnectedTo(output1));
                    Assert.That(input2, Is.ConnectedTo(output2));
                    return new DeleteElementsAction(edge0);
                },
                () =>
                {
                    RefreshReference(ref node0);
                    RefreshReference(ref node1);
                    RefreshReference(ref edge0);
                    input0 = node0.Input0;
                    input1 = node0.Input1;
                    input2 = node0.Input2;
                    output0 = node1.Output0;
                    output1 = node1.Output1;
                    output2 = node1.Output2;
                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GetEdgeCount(), Is.EqualTo(2));
                    Assert.That(input0, Is.Not.ConnectedTo(output0));
                    Assert.That(input1, Is.ConnectedTo(output1));
                    Assert.That(input2, Is.ConnectedTo(output2));
                });
        }

        [Test]
        public void Test_DeleteElementsAction_ManyEdges([Values] TestingMode mode)
        {
            var node0 = GraphModel.CreateNode<Type0FakeNodeModel>("Node0", new Vector2(-200, 0));
            var node1 = GraphModel.CreateNode<Type0FakeNodeModel>("Node1", new Vector2(200, 0));
            GraphModel.CreateEdge(node0.Input0, node1.Output0);
            GraphModel.CreateEdge(node0.Input1, node1.Output1);
            GraphModel.CreateEdge(node0.Input2, node1.Output2);

            TestPrereqActionPostreq(mode,
                () =>
                {
                    RefreshReference(ref node0);
                    RefreshReference(ref node1);
                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GetEdgeCount(), Is.EqualTo(3));
                    Assert.That(node0.Input0, Is.ConnectedTo(node1.Output0));
                    Assert.That(node0.Input1, Is.ConnectedTo(node1.Output1));
                    Assert.That(node0.Input2, Is.ConnectedTo(node1.Output2));
                    var edge0 = GraphModel.EdgeModels.First(e => PortModel.Equivalent(e.InputPortModel, node0.Input0));
                    return new DeleteElementsAction(edge0);
                },
                () =>
                {
                    RefreshReference(ref node0);
                    RefreshReference(ref node1);
                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GetEdgeCount(), Is.EqualTo(2));
                    Assert.That(node0.Input0, Is.Not.ConnectedTo(node1.Output0));
                    Assert.That(node0.Input1, Is.ConnectedTo(node1.Output1));
                    Assert.That(node0.Input2, Is.ConnectedTo(node1.Output2));
                });

            TestPrereqActionPostreq(mode,
                () =>
                {
                    RefreshReference(ref node0);
                    RefreshReference(ref node1);
                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GetEdgeCount(), Is.EqualTo(2));
                    Assert.That(node0.Input0, Is.Not.ConnectedTo(node1.Output0));
                    Assert.That(node0.Input1, Is.ConnectedTo(node1.Output1));
                    Assert.That(node0.Input2, Is.ConnectedTo(node1.Output2));
                    var edge1 = GraphModel.EdgeModels.First(e => PortModel.Equivalent(e.InputPortModel, node0.Input1));

                    return new DeleteElementsAction(edge1);
                },
                () =>
                {
                    RefreshReference(ref node0);
                    RefreshReference(ref node1);
                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GetEdgeCount(), Is.EqualTo(1));
                    Assert.That(node0.Input0, Is.Not.ConnectedTo(node1.Output0));
                    Assert.That(node0.Input1, Is.Not.ConnectedTo(node1.Output1));
                    Assert.That(node0.Input2, Is.ConnectedTo(node1.Output2));
                });

            TestPrereqActionPostreq(mode,
                () =>
                {
                    RefreshReference(ref node0);
                    RefreshReference(ref node1);
                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GetEdgeCount(), Is.EqualTo(1));
                    Assert.That(node0.Input0, Is.Not.ConnectedTo(node1.Output0));
                    Assert.That(node0.Input1, Is.Not.ConnectedTo(node1.Output1));
                    Assert.That(node0.Input2, Is.ConnectedTo(node1.Output2));
                    var edge2 = GraphModel.EdgeModels.First(e => PortModel.Equivalent(e.InputPortModel, node0.Input2));
                    return new DeleteElementsAction(edge2);
                },
                () =>
                {
                    RefreshReference(ref node0);
                    RefreshReference(ref node1);
                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(node0.Input0, Is.Not.ConnectedTo(node1.Output0));
                    Assert.That(node0.Input1, Is.Not.ConnectedTo(node1.Output1));
                    Assert.That(node0.Input2, Is.Not.ConnectedTo(node1.Output2));
                });
        }

        [Test]
        public void Test_SplitEdgeAndInsertNodeAction([Values] TestingMode mode)
        {
            var constant = GraphModel.CreateConstantNode("Constant", typeof(int).GenerateTypeHandle(Stencil), Vector2.zero);
            var binary0 = GraphModel.CreateBinaryOperatorNode(BinaryOperatorKind.Add, Vector2.zero);
            var binary1 = GraphModel.CreateBinaryOperatorNode(BinaryOperatorKind.Add, Vector2.zero);
            var edge = GraphModel.CreateEdge(binary0.InputPortA, constant.OutputPort);

            TestPrereqActionPostreq(mode,
                () =>
                {
                    RefreshReference(ref constant);
                    RefreshReference(ref binary0);
                    RefreshReference(ref binary1);
                    edge = GetEdge(0);
                    Assert.That(GetNodeCount(), Is.EqualTo(3));
                    Assert.That(GetEdgeCount(), Is.EqualTo(1));
                    Assert.That(binary0.InputPortA, Is.ConnectedTo(constant.OutputPort));
                    return new SplitEdgeAndInsertNodeAction(edge, binary1);
                },
                () =>
                {
                    RefreshReference(ref constant);
                    RefreshReference(ref binary0);
                    RefreshReference(ref binary1);
                    Assert.That(GetNodeCount(), Is.EqualTo(3));
                    Assert.That(GetEdgeCount(), Is.EqualTo(2));
                    Assert.That(binary1.InputPortA, Is.ConnectedTo(constant.OutputPort));
                    Assert.That(binary0.InputPortA, Is.ConnectedTo(binary1.OutputPort));
                });
        }

        [Test]
        public void TestCreateNodeOnEdge_OnlyInputPortConnected([Values] TestingMode mode)
        {
            var constant = GraphModel.CreateConstantNode("int", typeof(int).GenerateTypeHandle(Stencil), Vector2.zero);
            var unary = GraphModel.CreateUnaryOperatorNode(UnaryOperatorKind.Minus, Vector2.zero);
            var edge = GraphModel.CreateEdge(unary.InputPort, constant.OutputPort);

            var db = new GraphElementSearcherDatabase(Stencil).AddBinaryOperators().Build();
            var item = (GraphNodeModelSearcherItem)db.Search("equals", out _)[0];

            TestPrereqActionPostreq(mode,
                () =>
                {
                    RefreshReference(ref constant);
                    RefreshReference(ref unary);
                    edge = GetEdge(0);
                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GetEdgeCount(), Is.EqualTo(1));
                    Assert.That(unary.InputPort, Is.ConnectedTo(constant.OutputPort));
                    return new CreateNodeOnEdgeAction(edge, Vector2.zero, item);
                },
                () =>
                {
                    RefreshReference(ref constant);
                    RefreshReference(ref unary);
                    var binary = GraphModel.NodeModels.OfType<BinaryOperatorNodeModel>().FirstOrDefault();

                    Assert.IsNotNull(binary);
                    Assert.That(GetNodeCount(), Is.EqualTo(3));
                    Assert.That(GetEdgeCount(), Is.EqualTo(1));
                    Assert.That(constant.OutputPort, Is.ConnectedTo(binary.InputPortA));
                    Assert.That(constant.OutputPort, Is.Not.ConnectedTo(unary.InputPort));
                    Assert.That(binary.OutputPort, Is.Not.ConnectedTo(unary.InputPort));
                    Assert.That(unary.InputPort, Is.Not.ConnectedTo(constant.OutputPort));
                }
            );
        }

        [Test]
        public void TestCreateNodeOnEdge_BothPortsConnected([Values] TestingMode mode)
        {
            var constant = GraphModel.CreateConstantNode("int", typeof(int).GenerateTypeHandle(Stencil), Vector2.zero);
            var unary = GraphModel.CreateUnaryOperatorNode(UnaryOperatorKind.Minus, Vector2.zero);
            var edge = GraphModel.CreateEdge(unary.InputPort, constant.OutputPort);

            var db = new GraphElementSearcherDatabase(Stencil).AddUnaryOperators().Build();
            var item = (GraphNodeModelSearcherItem)db.Search("minus", out _)[0];

            TestPrereqActionPostreq(mode,
                () =>
                {
                    RefreshReference(ref unary);
                    RefreshReference(ref constant);
                    edge = GetEdge(0);
                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GetEdgeCount(), Is.EqualTo(1));
                    Assert.That(unary.InputPort, Is.ConnectedTo(constant.OutputPort));
                    return new CreateNodeOnEdgeAction(edge, Vector2.zero, item);
                },
                () =>
                {
                    RefreshReference(ref unary);
                    RefreshReference(ref constant);
                    RefreshReference(ref edge);
                    var unary2 = GraphModel.NodeModels.OfType<UnaryOperatorNodeModel>().ToList()[1];

                    Assert.IsNotNull(unary2);
                    Assert.That(GetNodeCount(), Is.EqualTo(3));
                    Assert.That(GetEdgeCount(), Is.EqualTo(2));
                    Assert.That(constant.OutputPort, Is.ConnectedTo(unary2.InputPort));
                    Assert.That(unary2.OutputPort, Is.ConnectedTo(unary.InputPort));
                    Assert.IsFalse(GraphModel.EdgeModels.Contains(edge));
                }
            );
        }

        [Test]
        public void TestCreateNodeOnEdge_WithOutputNodeConnectedToUnknown([Values] TestingMode mode)
        {
            var constantNode = GraphModel.CreateConstantNode("int1", typeof(int).GenerateTypeHandle(Stencil), Vector2.zero);
            var addNode = GraphModel.CreateBinaryOperatorNode(BinaryOperatorKind.Add, Vector2.zero);
            GraphModel.CreateEdge(addNode.InputPortA, constantNode.OutputPort);
            GraphModel.CreateEdge(addNode.InputPortB, constantNode.OutputPort);

            var db = new GraphElementSearcherDatabase(Stencil).AddBinaryOperators().Build();
            var item = (GraphNodeModelSearcherItem)db.Search("multiply", out _)[0];

            TestPrereqActionPostreq(mode,
                () =>
                {
                    RefreshReference(ref addNode);
                    RefreshReference(ref constantNode);
                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GetEdgeCount(), Is.EqualTo(2));

                    Assert.That(addNode, Is.Not.Null);
                    Assert.That(addNode.InputPortA, Is.ConnectedTo(constantNode.OutputPort));
                    var edge = GraphModel.EdgeModels.First();
                    return new CreateNodeOnEdgeAction(edge, Vector2.zero, item);
                },
                () =>
                {
                    RefreshReference(ref addNode);
                    RefreshReference(ref constantNode);
                    var multiplyNode = GraphModel.NodeModels.OfType<BinaryOperatorNodeModel>().ToList()[1];

                    Assert.IsNotNull(multiplyNode);
                    Assert.That(GetNodeCount(), Is.EqualTo(3));
                    Assert.That(GetEdgeCount(), Is.EqualTo(3));
                    Assert.That(constantNode.OutputPort, Is.ConnectedTo(multiplyNode.InputPortA));
                    Assert.That(multiplyNode.OutputPort, Is.ConnectedTo(addNode.InputPortA));
                    Assert.That(constantNode.OutputPort, Is.Not.ConnectedTo(addNode.InputPortA));
                }
            );
        }

        [Test]
        public void TestCreateEdge_CannotConnectForEachNodeToWhileStack()
        {
            var stack = GraphModel.CreateStack("", Vector2.zero);
            var forEach = stack.CreateStackedNode<ForEachNodeModel>();
            var whileStack = GraphModel.CreateLoopStack<WhileHeaderModel>(Vector2.zero);

            var edgeCount = GetEdgeCount();
            m_Store.Dispatch(new CreateEdgeAction(whileStack.InputPort, forEach.OutputPort));
            Assert.That(GetEdgeCount(), Is.EqualTo(edgeCount));
        }
    }
}
