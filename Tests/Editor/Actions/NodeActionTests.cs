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
                    return new CreateNodeAction(GraphModel, new Vector2(100, 200), item.CreateElements);
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
                    return new RemoveNodesAction(new INodeModel[] { nodeToDeleteAndBypass }, new INodeModel[] { nodeToDeleteAndBypass });
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
#if UNITY_2020_1_OR_NEWER
                    return new ChangeElementColorAction(Color.red, new[] { node0 }, null);
#else
                    return new ChangeElementColorAction(Color.red, new[] { node0 });
#endif
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
#if UNITY_2020_1_OR_NEWER
                    return new ChangeElementColorAction(Color.blue, new[] { Get(node1), Get(node2) }, null);
#else
                    return new ChangeElementColorAction(Color.blue, new[] { Get(node1), Get(node2) });
#endif
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
