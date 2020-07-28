using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.GraphElements;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting;
using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.GraphViewModel;
using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.SmartSearch;
using UnityEngine;

// ReSharper disable AccessToStaticMemberViaDerivedType

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.Actions
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
            var gedb = new GraphElementSearcherDatabase(Stencil, GraphModel);
            Type0FakeNodeModel.AddToSearcherDatabase(gedb);
            var db = gedb.Build();
            var item = (GraphNodeModelSearcherItem)db.Search(nameof(Type0FakeNodeModel), out _)[0];

            TestPrereqActionPostreq(mode,
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(0));
                    return new CreateNodeFromSearcherAction(new Vector2(100, 200), item, null);
                },
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(1));
                    Assert.That(GraphModel.NodeModels.First(), Is.TypeOf<Type0FakeNodeModel>());
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

                    var editorDataModel = m_Store.GetState().EditorDataModel;
                    VseGraphView.CopyPasteData copyPasteData = VseGraphView.GatherCopiedElementsData(new List<IGTFGraphElementModel> { nodeModel });

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
            var const0 = GraphModel.CreateConstantNode("const0", typeof(float).GenerateTypeHandle(), Vector2.zero);
            var const1 = GraphModel.CreateConstantNode("const1", typeof(float).GenerateTypeHandle(), Vector2.zero);
            var const2 = GraphModel.CreateConstantNode("const2", typeof(float).GenerateTypeHandle(), Vector2.zero);
            var const3 = GraphModel.CreateConstantNode("const3", typeof(float).GenerateTypeHandle(), Vector2.zero);
            var const4 = GraphModel.CreateConstantNode("const4", typeof(float).GenerateTypeHandle(), Vector2.zero);
            var const5 = GraphModel.CreateConstantNode("const5", typeof(float).GenerateTypeHandle(), Vector2.zero);
            var binary0 = GraphModel.CreateNode<Type0FakeNodeModel>("Node0", Vector2.zero);
            var binary1 = GraphModel.CreateNode<Type0FakeNodeModel>("Node1", Vector2.zero);
            var binary2 = GraphModel.CreateNode<Type0FakeNodeModel>("Node2", Vector2.zero);
            var binary3 = GraphModel.CreateNode<Type0FakeNodeModel>("Node3", Vector2.zero);
            GraphModel.CreateEdge(binary0.Input0, const0.OutputPort);
            GraphModel.CreateEdge(binary0.Input1, const1.OutputPort);
            GraphModel.CreateEdge(binary1.Input0, binary0.Output0);
            GraphModel.CreateEdge(binary1.Input1, const0.OutputPort);
            GraphModel.CreateEdge(binary2.Input0, const2.OutputPort);
            GraphModel.CreateEdge(binary2.Input1, const3.OutputPort);
            GraphModel.CreateEdge(binary3.Input0, const4.OutputPort);
            GraphModel.CreateEdge(binary3.Input1, const5.OutputPort);

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
            var constantA = GraphModel.CreateConstantNode("constantA", typeof(int).GenerateTypeHandle(), Vector2.zero);
            var binary0 = GraphModel.CreateNode<Type0FakeNodeModel>("Node1", Vector2.zero);
            var binary1 = GraphModel.CreateNode<Type0FakeNodeModel>("Node2", Vector2.zero);
            IGTFPortModel outputPort = constantA.OutputPort;
            GraphModel.CreateEdge(binary0.Input0, outputPort);
            IGTFPortModel outputPort1 = binary0.Output0;
            GraphModel.CreateEdge(binary1.Input0, outputPort1);

            TestPrereqActionPostreq(mode,
                () =>
                {
                    Refresh();
                    var nodeToDeleteAndBypass = GraphModel.NodeModels.OfType<Type0FakeNodeModel>().First();

                    Assert.That(GetNodeCount(), Is.EqualTo(3));
                    Assert.That(GetEdgeCount(), Is.EqualTo(2));
                    Assert.That(nodeToDeleteAndBypass.Input0, Is.ConnectedTo(constantA.OutputPort));
                    Assert.That(binary1.Input0, Is.ConnectedTo(nodeToDeleteAndBypass.Output0));
                    return new RemoveNodesAction(new IInOutPortsNode[] {nodeToDeleteAndBypass}, new IGTFNodeModel[] {nodeToDeleteAndBypass});
                },
                () =>
                {
                    Refresh();
                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GetEdgeCount(), Is.EqualTo(1));
                    Assert.That(binary1.Input0, Is.ConnectedTo(constantA.OutputPort));
                });

            void Refresh()
            {
                RefreshReference(ref binary0);
                RefreshReference(ref binary1);
                RefreshReference(ref constantA);
            }
        }

        T Get<T>(T prev) where T : IGTFNodeModel
        {
            return (T)GraphModel.NodesByGuid[prev.Guid];
        }

        [Test]
        public void Test_ChangeNodeColorAction([Values] TestingMode mode)
        {
            var node0 = GraphModel.CreateNode<Type0FakeNodeModel>("Node0", Vector2.zero);
            var node1 = GraphModel.CreateNode<Type0FakeNodeModel>("Node1", Vector2.zero);
            var node2 = GraphModel.CreateNode<Type0FakeNodeModel>("Node2", Vector2.zero);
            var node3 = GraphModel.CreateNode<Type0FakeNodeModel>("Node3", Vector2.zero);

            TestPrereqActionPostreq(mode,
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(4));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(Get(node0).Color, Is.EqualTo(Color.clear));
                    Assert.That(Get(node1).Color, Is.EqualTo(Color.clear));
                    Assert.That(Get(node2).Color, Is.EqualTo(Color.clear));
                    Assert.That(Get(node3).Color, Is.EqualTo(Color.clear));
                    return new ChangeElementColorAction(Color.red, new[] { node0 }, null);
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
                    return new ChangeElementColorAction(Color.blue, new[] { Get(node1), Get(node2) }, null);
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
