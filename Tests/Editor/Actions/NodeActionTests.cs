using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor.VisualScripting.Editor;
using UnityEditor.VisualScripting.Editor.SmartSearch;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model;
using UnityEditor.VisualScripting.Model.Stencils;
using UnityEngine;

// ReSharper disable AccessToStaticMemberViaDerivedType

namespace UnityEditor.VisualScriptingTests.Actions
{
    [Category("Node")]
    [Category("Action")]
    class NodeActionTests : BaseFixture
    {
        protected override bool CreateGraphOnStartup => true;
        protected override Type CreatedGraphType => typeof(ClassStencil);

        [Test]
        public void Test_CreateNodeFromSearcherAction([Values] TestingMode mode)
        {
            var db = new GraphElementSearcherDatabase(Stencil).AddBinaryOperators().Build();
            var item = (GraphNodeModelSearcherItem)db.Search("add", out _)[0];

            TestPrereqActionPostreq(mode,
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(0));
                    return new CreateNodeFromSearcherAction(GraphModel, new Vector2(100, 200), item);
                },
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(1));
                    Assert.That(GraphModel.NodeModels.First(), Is.TypeOf<BinaryOperatorNodeModel>());
                    Assert.That(GraphModel.NodeModels.First().Position,
                        Is.EqualTo(new Vector2(100, 200)));
                }
            );
        }

        [Test]
        public void Test_DuplicateAction_OneNode([Values] TestingMode mode)
        {
            GraphModel.CreateNode<Type0FakeNodeModel>("Node0", Vector2.zero);
            TestPrereqActionPostreq(mode,
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(1));
                    var nodeModel = GetNode(0);
                    Assert.That(nodeModel, Is.TypeOf<Type0FakeNodeModel>());

                    TargetInsertionInfo info = new TargetInsertionInfo();
                    info.OperationName = "Duplicate";
                    info.Delta = Vector2.one;
                    info.TargetStackInsertionIndex = -1;

                    IEditorDataModel editorDataModel = m_Store.GetState().EditorDataModel;
                    VseGraphView.CopyPasteData copyPasteData = VseGraphView.GatherCopiedElementsData(new List<IGraphElementModel> { nodeModel });

                    return new PasteSerializedDataAction(GraphModel, info, editorDataModel, copyPasteData);
                },
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GraphModel.NodeModels.Count(n => n == null), Is.Zero);
                });
        }

        [Test]
        public void Test_DeleteElementsAction_OneNode([Values] TestingMode mode)
        {
            GraphModel.CreateNode<Type0FakeNodeModel>("Node0", Vector2.zero);
            TestPrereqActionPostreq(mode,
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(1));
                    Assert.That(GetNode(0), Is.TypeOf<Type0FakeNodeModel>());
                    return new DeleteElementsAction(GetNode(0));
                },
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(0));
                });
        }

        [Test]
        public void Test_DeleteElementsAction_ManyNodesSequential([Values] TestingMode mode)
        {
            GraphModel.CreateNode<Type0FakeNodeModel>("Node0", Vector2.zero);
            GraphModel.CreateNode<Type0FakeNodeModel>("Node1", Vector2.zero);
            GraphModel.CreateNode<Type0FakeNodeModel>("Node2", Vector2.zero);

            TestPrereqActionPostreq(mode,
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(3));
                    Assert.That(GetNode(0), Is.TypeOf<Type0FakeNodeModel>());
                    return new DeleteElementsAction(GetNode(0));
                },
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                });

            TestPrereqActionPostreq(mode,
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GetNode(0), Is.TypeOf<Type0FakeNodeModel>());
                    return new DeleteElementsAction(GetNode(0));
                },
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(1));
                });

            TestPrereqActionPostreq(mode,
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(1));
                    Assert.That(GetNode(0), Is.TypeOf<Type0FakeNodeModel>());
                    return new DeleteElementsAction(GetNode(0));
                },
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(0));
                });
        }

        [Test]
        public void Test_DeleteElementsAction_ManyNodesSameTime([Values] TestingMode mode)
        {
            GraphModel.CreateNode<Type0FakeNodeModel>("Node0", Vector2.zero);
            GraphModel.CreateNode<Type0FakeNodeModel>("Node1", Vector2.zero);
            GraphModel.CreateNode<Type0FakeNodeModel>("Node2", Vector2.zero);

            TestPrereqActionPostreq(mode,
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(3));
                    Assert.That(GetNode(0), Is.TypeOf<Type0FakeNodeModel>());
                    return new DeleteElementsAction(GetNode(0), GetNode(1), GetNode(2));
                },
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(0));
                });
        }

        [Test]
        public void Test_DisconnectNodeAction([Values] TestingMode mode)
        {
            var const0 = GraphModel.CreateConstantNode("const0", typeof(float).GenerateTypeHandle(Stencil), Vector2.zero);
            var const1 = GraphModel.CreateConstantNode("const1", typeof(float).GenerateTypeHandle(Stencil), Vector2.zero);
            var const2 = GraphModel.CreateConstantNode("const2", typeof(float).GenerateTypeHandle(Stencil), Vector2.zero);
            var const3 = GraphModel.CreateConstantNode("const3", typeof(float).GenerateTypeHandle(Stencil), Vector2.zero);
            var const4 = GraphModel.CreateConstantNode("const4", typeof(float).GenerateTypeHandle(Stencil), Vector2.zero);
            var const5 = GraphModel.CreateConstantNode("const5", typeof(float).GenerateTypeHandle(Stencil), Vector2.zero);
            var binary0 = GraphModel.CreateBinaryOperatorNode(BinaryOperatorKind.Add, Vector2.zero);
            var binary1 = GraphModel.CreateBinaryOperatorNode(BinaryOperatorKind.Add, Vector2.zero);
            var binary2 = GraphModel.CreateBinaryOperatorNode(BinaryOperatorKind.Add, Vector2.zero);
            var binary3 = GraphModel.CreateBinaryOperatorNode(BinaryOperatorKind.Add, Vector2.zero);
            GraphModel.CreateEdge(binary0.InputPortA, const0.OutputPort);
            GraphModel.CreateEdge(binary0.InputPortB, const1.OutputPort);
            GraphModel.CreateEdge(binary1.InputPortA, binary0.OutputPort);
            GraphModel.CreateEdge(binary1.InputPortB, const0.OutputPort);
            GraphModel.CreateEdge(binary2.InputPortA, const2.OutputPort);
            GraphModel.CreateEdge(binary2.InputPortB, const3.OutputPort);
            GraphModel.CreateEdge(binary3.InputPortA, const4.OutputPort);
            GraphModel.CreateEdge(binary3.InputPortB, const5.OutputPort);

            TestPrereqActionPostreq(mode,
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(10));
                    Assert.That(GetEdgeCount(), Is.EqualTo(8));
                    return new DisconnectNodeAction(binary0);
                },
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(10));
                    Assert.That(GetEdgeCount(), Is.EqualTo(5));
                });

            TestPrereqActionPostreq(mode,
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(10));
                    Assert.That(GetEdgeCount(), Is.EqualTo(5));
                    return new DisconnectNodeAction(binary2, binary3);
                },
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(10));
                    Assert.That(GetEdgeCount(), Is.EqualTo(1));
                });
        }

        [Test]
        public void Test_BypassNodeAction([Values] TestingMode mode)
        {
            var constantA = GraphModel.CreateConstantNode("constantA", typeof(float).GenerateTypeHandle(Stencil), Vector2.zero);
            var binary0 = GraphModel.CreateBinaryOperatorNode(BinaryOperatorKind.Add, Vector2.zero);
            var binary1 = GraphModel.CreateBinaryOperatorNode(BinaryOperatorKind.Add, Vector2.zero);
            GraphModel.CreateEdge(binary0.InputPortA, constantA.OutputPort);
            GraphModel.CreateEdge(binary1.InputPortA, binary0.OutputPort);

            var constantB = GraphModel.CreateConstantNode("constantB", typeof(float).GenerateTypeHandle(Stencil), Vector2.zero);
            var binary2 = GraphModel.CreateBinaryOperatorNode(BinaryOperatorKind.Add, Vector2.zero);
            var binary3 = GraphModel.CreateBinaryOperatorNode(BinaryOperatorKind.Add, Vector2.zero);
            GraphModel.CreateEdge(binary2.InputPortA, constantB.OutputPort);
            GraphModel.CreateEdge(binary3.InputPortA, binary2.OutputPort);

            var constantC = GraphModel.CreateConstantNode("constantC", typeof(float).GenerateTypeHandle(Stencil), Vector2.zero);
            var binary4 = GraphModel.CreateBinaryOperatorNode(BinaryOperatorKind.Add, Vector2.zero);
            var binary5 = GraphModel.CreateBinaryOperatorNode(BinaryOperatorKind.Add, Vector2.zero);
            GraphModel.CreateEdge(binary4.InputPortA, constantC.OutputPort);
            GraphModel.CreateEdge(binary5.InputPortA, binary4.OutputPort);

            TestPrereqActionPostreq(mode,
                () =>
                {
                    Refresh();
                    Assert.That(GetNodeCount(), Is.EqualTo(9));
                    Assert.That(GetEdgeCount(), Is.EqualTo(6));
                    Assert.That(binary0.InputPortA, Is.ConnectedTo(constantA.OutputPort));
                    Assert.That(binary1.InputPortA, Is.ConnectedTo(binary0.OutputPort));
                    Assert.That(binary2.InputPortA, Is.ConnectedTo(constantB.OutputPort));
                    Assert.That(binary3.InputPortA, Is.ConnectedTo(binary2.OutputPort));
                    Assert.That(binary4.InputPortA, Is.ConnectedTo(constantC.OutputPort));
                    Assert.That(binary5.InputPortA, Is.ConnectedTo(binary4.OutputPort));
                    return new BypassNodeAction(binary0);
                },
                () =>
                {
                    Refresh();
                    Assert.That(GetNodeCount(), Is.EqualTo(9));
                    Assert.That(GetEdgeCount(), Is.EqualTo(5));
                    Assert.That(binary1.InputPortA, Is.ConnectedTo(constantA.OutputPort));
                    Assert.That(binary2.InputPortA, Is.ConnectedTo(constantB.OutputPort));
                    Assert.That(binary3.InputPortA, Is.ConnectedTo(binary2.OutputPort));
                    Assert.That(binary4.InputPortA, Is.ConnectedTo(constantC.OutputPort));
                    Assert.That(binary5.InputPortA, Is.ConnectedTo(binary4.OutputPort));
                });

            TestPrereqActionPostreq(mode,
                () =>
                {
                    Refresh();
                    Assert.That(GetNodeCount(), Is.EqualTo(9));
                    Assert.That(GetEdgeCount(), Is.EqualTo(5));
                    Assert.That(binary1.InputPortA, Is.ConnectedTo(constantA.OutputPort));
                    Assert.That(binary2.InputPortA, Is.ConnectedTo(constantB.OutputPort));
                    Assert.That(binary3.InputPortA, Is.ConnectedTo(binary2.OutputPort));
                    Assert.That(binary4.InputPortA, Is.ConnectedTo(constantC.OutputPort));
                    Assert.That(binary5.InputPortA, Is.ConnectedTo(binary4.OutputPort));
                    return new BypassNodeAction(binary2, binary4);
                },
                () =>
                {
                    Refresh();
                    Assert.That(GetNodeCount(), Is.EqualTo(9));
                    Assert.That(GetEdgeCount(), Is.EqualTo(3));
                    Assert.That(binary1.InputPortA, Is.ConnectedTo(constantA.OutputPort));
                    Assert.That(binary3.InputPortA, Is.ConnectedTo(constantB.OutputPort));
                    Assert.That(binary5.InputPortA, Is.ConnectedTo(constantC.OutputPort));
                });

            void Refresh()
            {
                RefreshReference(ref binary0);
                RefreshReference(ref binary1);
                RefreshReference(ref binary2);
                RefreshReference(ref binary3);
                RefreshReference(ref binary4);
                RefreshReference(ref binary5);
                RefreshReference(ref constantA);
                RefreshReference(ref constantB);
                RefreshReference(ref constantC);
            }
        }

        [Test]
        public void Test_RemoveNodesAction([Values] TestingMode mode)
        {
            var constantA = GraphModel.CreateConstantNode("constantA", typeof(int).GenerateTypeHandle(Stencil), Vector2.zero);
            var binary0 = GraphModel.CreateNode<Type0FakeNodeModel>("Node1", Vector2.zero);
            var binary1 = GraphModel.CreateBinaryOperatorNode(BinaryOperatorKind.Add, Vector2.zero);
            IPortModel outputPort = constantA.OutputPort;
            GraphModel.CreateEdge(binary0.Input0, outputPort);
            IPortModel outputPort1 = binary0.Output0;
            GraphModel.CreateEdge(binary1.InputPortA, outputPort1);

            TestPrereqActionPostreq(mode,
                () =>
                {
                    Refresh();
                    var nodeToDeleteAndBypass = GraphModel.NodeModels.OfType<Type0FakeNodeModel>().First();

                    Assert.That(GetNodeCount(), Is.EqualTo(3));
                    Assert.That(GetEdgeCount(), Is.EqualTo(2));
                    Assert.That(nodeToDeleteAndBypass.Input0, Is.ConnectedTo(constantA.OutputPort));
                    Assert.That(binary1.InputPortA, Is.ConnectedTo(nodeToDeleteAndBypass.Output0));
                    return new RemoveNodesAction(new INodeModel[] {nodeToDeleteAndBypass}, new INodeModel[] {nodeToDeleteAndBypass});
                },
                () =>
                {
                    Refresh();
                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GetEdgeCount(), Is.EqualTo(1));
                    Assert.That(binary1.InputPortA, Is.ConnectedTo(constantA.OutputPort));
                });

            void Refresh()
            {
                RefreshReference(ref binary0);
                RefreshReference(ref binary1);
                RefreshReference(ref constantA);
            }
        }

        //TODO We disabled exception to fix a bug where Bypass&Remove would throw when removing a group of nodes...
        //     where one of the nodes (ex:constant) has only one edge connected to the group and that edge is removed
        //     before being doing the bypass on the constant node. See the fogbugz case 1049559
        [Ignore("Disable until remove corner cases handled")]
        [Test]
        public void Test_BypassNodeAction_Throw()
        {
            var constantA = GraphModel.CreateConstantNode("constantA", typeof(float).GenerateTypeHandle(Stencil), Vector2.zero);

            Assert.Throws<InvalidOperationException>(() => m_Store.Dispatch(new BypassNodeAction(constantA)));
        }

        T Get<T>(T prev) where T : INodeModel
        {
            return (T)GraphModel.NodesByGuid[prev.Guid];
        }

        [Test]
        public void Test_ChangeNodeColorAction([Values] TestingMode mode)
        {
            var node0 = GraphModel.CreateBinaryOperatorNode(BinaryOperatorKind.Add, Vector2.zero);
            var node1 = GraphModel.CreateBinaryOperatorNode(BinaryOperatorKind.Add, Vector2.zero);
            var node2 = GraphModel.CreateBinaryOperatorNode(BinaryOperatorKind.Add, Vector2.zero);
            var node3 = GraphModel.CreateBinaryOperatorNode(BinaryOperatorKind.Add, Vector2.zero);

            TestPrereqActionPostreq(mode,
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(4));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(Get(node0).Color, Is.EqualTo(Color.clear));
                    Assert.That(Get(node1).Color, Is.EqualTo(Color.clear));
                    Assert.That(Get(node2).Color, Is.EqualTo(Color.clear));
                    Assert.That(Get(node3).Color, Is.EqualTo(Color.clear));
                    return new ChangeNodeColorAction(Color.red, node0);
                },
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(4));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(Get(node0).Color, Is.EqualTo(Color.red));
                    Assert.That(Get(node1).Color, Is.EqualTo(Color.clear));
                    Assert.That(Get(node2).Color, Is.EqualTo(Color.clear));
                    Assert.That(Get(node3).Color, Is.EqualTo(Color.clear));
                });

            TestPrereqActionPostreq(mode,
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(4));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(Get(node0).Color, Is.EqualTo(Color.red));
                    Assert.That(Get(node1).Color, Is.EqualTo(Color.clear));
                    Assert.That(Get(node2).Color, Is.EqualTo(Color.clear));
                    Assert.That(Get(node3).Color, Is.EqualTo(Color.clear));
                    return new ChangeNodeColorAction(Color.blue, Get(node1), Get(node2));
                },
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(4));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(Get(node0).Color, Is.EqualTo(Color.red));
                    Assert.That(Get(node1).Color, Is.EqualTo(Color.blue));
                    Assert.That(Get(node2).Color, Is.EqualTo(Color.blue));
                    Assert.That(Get(node3).Color, Is.EqualTo(Color.clear));
                });
        }
    }
}
