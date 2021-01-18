using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEngine;

// ReSharper disable AccessToStaticMemberViaDerivedType

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.Actions
{
    [Category("Variable")]
    [Category("Action")]
    class VariableActionTests : BaseFixture
    {
        protected override bool CreateGraphOnStartup => true;
        protected override Type CreatedGraphType => typeof(ClassStencil);

        [Test]
        public void Test_CreateGraphVariableDeclarationAction_PreservesModifierFlags([Values] TestingMode mode, [Values] ModifierFlags flags)
        {
            TestPrereqActionPostreq(mode,
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(0));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(GetVariableDeclarationCount(), Is.EqualTo(0));
                    return new CreateGraphVariableDeclarationAction("toto", true, typeof(int).GenerateTypeHandle(), flags);
                },
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(0));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(GetVariableDeclarationCount(), Is.EqualTo(1));
                    Assert.That(GetVariableDeclaration(0).DataType.Resolve(), Is.EqualTo(typeof(int)));
                    Assert.That(GetVariableDeclaration(0).Modifiers, Is.EqualTo(flags));
                });
        }

        [Test]
        public void Test_CreateGraphVariableDeclarationAction([Values] TestingMode mode)
        {
            TestPrereqActionPostreq(mode,
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(0));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(GetVariableDeclarationCount(), Is.EqualTo(0));
                    return new CreateGraphVariableDeclarationAction("toto", true, typeof(int).GenerateTypeHandle());
                },
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(0));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(GetVariableDeclarationCount(), Is.EqualTo(1));
                    Assert.That(GetVariableDeclaration(0).DataType.Resolve(), Is.EqualTo(typeof(int)));
                });

            TestPrereqActionPostreq(mode,
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(0));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(GetVariableDeclarationCount(), Is.EqualTo(1));
                    return new CreateGraphVariableDeclarationAction("foo", true, typeof(float).GenerateTypeHandle());
                },
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(0));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(GetVariableDeclarationCount(), Is.EqualTo(2));
                    Assert.That(GetVariableDeclaration(0).DataType.Resolve(), Is.EqualTo(typeof(int)));
                    Assert.That(GetVariableDeclaration(1).DataType.Resolve(), Is.EqualTo(typeof(float)));
                });
        }

        [Test]
        public void Test_CreateVariableNodeAction([Values] TestingMode mode)
        {
            var declaration = GraphModel.CreateGraphVariableDeclaration(typeof(int).GenerateTypeHandle(), "decl0", ModifierFlags.None, true);

            TestPrereqActionPostreq(mode,
                () =>
                {
                    declaration = GetVariableDeclaration(0);
                    Assert.That(GetNodeCount(), Is.EqualTo(0));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(GetVariableDeclarationCount(), Is.EqualTo(1));
                    return new CreateVariableNodesAction(declaration, Vector2.zero);
                },
                () =>
                {
                    declaration = GetVariableDeclaration(0);
                    Assert.That(GetNodeCount(), Is.EqualTo(1));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(GetVariableDeclarationCount(), Is.EqualTo(1));
                    Assert.That(GetNode(0), Is.TypeOf<VariableNodeModel>());
                });

            TestPrereqActionPostreq(mode,
                () =>
                {
                    declaration = GetVariableDeclaration(0);
                    Assert.That(GetNodeCount(), Is.EqualTo(1));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(GetVariableDeclarationCount(), Is.EqualTo(1));
                    return new CreateVariableNodesAction(declaration, Vector2.zero);
                },
                () =>
                {
                    declaration = GetVariableDeclaration(0);
                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(GetVariableDeclarationCount(), Is.EqualTo(1));
                    Assert.That(GetNode(0), Is.TypeOf<VariableNodeModel>());
                });
        }

        [Test]
        public void Test_DeleteElementsAction_VariableUsage([Values] TestingMode mode)
        {
            var declaration0 = GraphModel.CreateGraphVariableDeclaration(typeof(int).GenerateTypeHandle(), "decl0", ModifierFlags.None, true);
            var declaration1 = GraphModel.CreateGraphVariableDeclaration(typeof(int).GenerateTypeHandle(), "decl1", ModifierFlags.None, true);

            var node0 = GraphModel.CreateVariableNode(declaration0, Vector2.zero);
            var node1 = GraphModel.CreateVariableNode(declaration0, Vector2.zero);
            var node2 = GraphModel.CreateVariableNode(declaration1, Vector2.zero);
            var node3 = GraphModel.CreateVariableNode(declaration1, Vector2.zero);
            var node4 = GraphModel.CreateNode<Type0FakeNodeModel>("Node0", Vector2.zero);
            var node5 = GraphModel.CreateNode<Type0FakeNodeModel>("Node1", Vector2.zero);
            GraphModel.CreateEdge(node4.Input0, node0.OutputPort);
            GraphModel.CreateEdge(node4.Input1, node2.OutputPort);
            GraphModel.CreateEdge(node5.Input0, node1.OutputPort);
            GraphModel.CreateEdge(node5.Input1, node3.OutputPort);

            TestPrereqActionPostreq(mode,
                () =>
                {
                    declaration0 = GetVariableDeclaration(0);
                    declaration1 = GetVariableDeclaration(1);
                    Assert.That(GetNodeCount(), Is.EqualTo(6), "GetNodeCount1");
                    Assert.That(GetEdgeCount(), Is.EqualTo(4));
                    Assert.That(GetVariableDeclarationCount(), Is.EqualTo(2), "GetVariableDeclarationCount");
                    return new DeleteElementsAction(new[] { declaration0 });
                },
                () =>
                {
                    declaration1 = GetVariableDeclaration(0);
                    Assert.That(GetNodeCount(), Is.EqualTo(4), "GetNodeCount2");
                    Assert.That(GetEdgeCount(), Is.EqualTo(2), "EdgeCount");
                    Assert.That(GetVariableDeclarationCount(), Is.EqualTo(1), "GetVariableDeclarationCount");
                });

            TestPrereqActionPostreq(mode,
                () =>
                {
                    declaration1 = GetVariableDeclaration(0);
                    Assert.That(GetNodeCount(), Is.EqualTo(4), "GetNodeCount3");
                    Assert.That(GetEdgeCount(), Is.EqualTo(2), "EdgeCount");
                    Assert.That(GetVariableDeclarationCount(), Is.EqualTo(1), "GetVariableDeclarationCount");
                    return new DeleteElementsAction(new[] { declaration1 });
                },
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(2), "GetNodeCount");
                    Assert.That(GetEdgeCount(), Is.EqualTo(0), "EdgeCount");
                    Assert.That(GetVariableDeclarationCount(), Is.EqualTo(0));
                });
        }

        [Test]
        public void Test_RenameGraphVariableDeclarationAction([Values] TestingMode mode)
        {
            var variable = GraphModel.CreateGraphVariableDeclaration(typeof(int).GenerateTypeHandle(), "toto", ModifierFlags.None, true);

            TestPrereqActionPostreq(mode,
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(0));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(GetVariableDeclarationCount(), Is.EqualTo(1));
                    Assert.That(GetVariableDeclaration(0).Title, Is.EqualTo("toto"));
                    return new RenameElementAction(variable as IRenamable, "foo");
                },
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(0));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(GetVariableDeclarationCount(), Is.EqualTo(1));
                    Assert.That(GetVariableDeclaration(0).Title, Is.EqualTo("foo"));
                });
        }

        [Test]
        [TestCase(TestingMode.Action, 0, 2, new[] {1, 2, 0, 3})]
        [TestCase(TestingMode.UndoRedo, 0, 2, new[] {1, 2, 0, 3})]
        [TestCase(TestingMode.Action, 0, 0, new[] {0, 1, 2, 3})]
        [TestCase(TestingMode.Action, 0, 0, new[] {0, 1, 2, 3})]
        [TestCase(TestingMode.Action, 3, 3, new[] {0, 1, 2, 3})]
        [TestCase(TestingMode.Action, 0, 3, new[] {1, 2, 3, 0})]
        [TestCase(TestingMode.Action, 3, 0, new[] {0, 3, 1, 2})]
        [TestCase(TestingMode.Action, 1, 1, new[] {0, 1, 2, 3})]
        [TestCase(TestingMode.Action, 1, 2, new[] {0, 2, 1, 3})]
        public void Test_ReorderGraphVariableDeclarationAction(TestingMode mode, int indexToMove, int afterWhich, int[] expectedOrder)
        {
            var declaration0 = GraphModel.CreateGraphVariableDeclaration(typeof(int).GenerateTypeHandle(), "decl0", ModifierFlags.None, true);
            var declaration1 = GraphModel.CreateGraphVariableDeclaration(typeof(int).GenerateTypeHandle(), "decl1", ModifierFlags.None, true);
            var declaration2 = GraphModel.CreateGraphVariableDeclaration(typeof(int).GenerateTypeHandle(), "decl2", ModifierFlags.None, true);
            var declaration3 = GraphModel.CreateGraphVariableDeclaration(typeof(int).GenerateTypeHandle(), "decl3", ModifierFlags.None, true);

            var declarations = new[] { declaration0, declaration1, declaration2, declaration3 };

            TestPrereqActionPostreq(mode,
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(0));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(GetVariableDeclarationCount(), Is.EqualTo(4));
                    Assert.That(GetVariableDeclaration(0).Guid, Is.EqualTo(declaration0.Guid));
                    Assert.That(GetVariableDeclaration(1).Guid, Is.EqualTo(declaration1.Guid));
                    Assert.That(GetVariableDeclaration(2).Guid, Is.EqualTo(declaration2.Guid));
                    Assert.That(GetVariableDeclaration(3).Guid, Is.EqualTo(declaration3.Guid));
                    return new ReorderGraphVariableDeclarationAction(new[] { declarations[indexToMove] }, declarations[afterWhich]);
                },
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(0));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(GetVariableDeclarationCount(), Is.EqualTo(4));
                    Assert.That(GetVariableDeclaration(0).Guid, Is.EqualTo(declarations[expectedOrder[0]].Guid));
                    Assert.That(GetVariableDeclaration(1).Guid, Is.EqualTo(declarations[expectedOrder[1]].Guid));
                    Assert.That(GetVariableDeclaration(2).Guid, Is.EqualTo(declarations[expectedOrder[2]].Guid));
                    Assert.That(GetVariableDeclaration(3).Guid, Is.EqualTo(declarations[expectedOrder[3]].Guid));
                });
        }

        [Test]
        public void Test_ConvertVariableNodeToConstantNodeAction([Values] TestingMode mode)
        {
            var declaration0 = GraphModel.CreateGraphVariableDeclaration(typeof(int).GenerateTypeHandle(), "decl0", ModifierFlags.None, true);
            var node0 = GraphModel.CreateNode<Type0FakeNodeModel>("Node0", Vector2.zero);
            var node1 = GraphModel.CreateVariableNode(declaration0, Vector2.zero);
            IPortModel outputPort = node1.OutputPort;
            Color modelColor = Color.red;
            ModelState modelState = ModelState.Disabled;
            GraphModel.CreateEdge(node0.Input0, outputPort);

            TestPrereqActionPostreq(mode,
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GetEdgeCount(), Is.EqualTo(1));
                    Assert.That(GetVariableDeclarationCount(), Is.EqualTo(1));
                    Assert.That(GetNode(1), Is.TypeOf<VariableNodeModel>());
                    var n0 = (Type0FakeNodeModel)GetNode(0);
                    var n1 = (VariableNodeModel)GetNode(1);
                    n1.Color = modelColor;
                    n1.State = modelState;
                    Assert.That(n0.Input0, Is.ConnectedTo(n1.OutputPort));
                    return new ConvertVariableNodesToConstantNodesAction(node1);
                },
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GetEdgeCount(), Is.EqualTo(1));
                    Assert.That(GetVariableDeclarationCount(), Is.EqualTo(1));
                    Assert.That(GetConstantNode(1), Is.TypeOf<IntConstant>());

                    var n0 = (Type0FakeNodeModel)GetNode(0);
                    var n1 = (ConstantNodeModel)GetNode(1);
                    Assert.That(n0.Input0, Is.ConnectedTo(n1.OutputPort));
                    Assert.That(n1.Color, Is.EqualTo(modelColor));
                    Assert.That(n1.State, Is.EqualTo(modelState));
                });
        }

        [Test]
        public void Test_ConvertConstantNodeToVariableNodeAction([Values] TestingMode mode)
        {
            var binary = GraphModel.CreateNode<Type0FakeNodeModel>("Node0", Vector2.zero);
            var constant = GraphModel.CreateConstantNode(typeof(int).GenerateTypeHandle(), "const0", Vector2.zero);
            IPortModel outputPort = constant.OutputPort;
            Color modelColor = Color.red;
            ModelState modelState = ModelState.Disabled;
            GraphModel.CreateEdge(binary.Input0, outputPort);

            TestPrereqActionPostreq(mode,
                () =>
                {
                    var c = GraphModel.NodeModels.OfType<IConstantNodeModel>().First();
                    c.Color = modelColor;
                    c.State = modelState;

                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GetEdgeCount(), Is.EqualTo(1));
                    Assert.That(GetVariableDeclarationCount(), Is.EqualTo(0));
                    Assert.That(GetConstantNode(1), Is.TypeOf<IntConstant>());

                    var n0 = (Type0FakeNodeModel)GetNode(0);
                    var n1 = (ConstantNodeModel)GetNode(1);
                    Assert.That(n0.Input0, Is.ConnectedTo(n1.OutputPort));
                    return new ConvertConstantNodesToVariableNodesAction(c);
                },
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GetEdgeCount(), Is.EqualTo(1));
                    Assert.That(GetVariableDeclarationCount(), Is.EqualTo(1));
                    Assert.That(GetNode(1), Is.TypeOf<VariableNodeModel>());

                    var n0 = (Type0FakeNodeModel)GetNode(0);
                    var n1 = (VariableNodeModel)GetNode(1);
                    Assert.That(n0.Input0, Is.ConnectedTo(n1.OutputPort));
                    Assert.That(n1.GetDataType(), Is.EqualTo(typeof(int).GenerateTypeHandle()));
                    Assert.That(n1.Color, Is.EqualTo(modelColor));
                    Assert.That(n1.State, Is.EqualTo(modelState));
                });
        }

        [Test]
        public void Test_ItemizeVariableNodeAction([Values] TestingMode mode)
        {
            var declaration = GraphModel.CreateGraphVariableDeclaration(typeof(int).GenerateTypeHandle(), "decl0", ModifierFlags.None, true);
            var variable = GraphModel.CreateVariableNode(declaration, Vector2.zero);
            var binary0 = GraphModel.CreateNode<Type0FakeNodeModel>("Node0", Vector2.zero);
            var binary1 = GraphModel.CreateNode<Type0FakeNodeModel>("Node1", Vector2.zero);

            IPortModel outputPort = variable.OutputPort;
            GraphModel.CreateEdge(binary0.Input0, outputPort);
            IPortModel outputPort1 = variable.OutputPort;
            GraphModel.CreateEdge(binary0.Input1, outputPort1);
            IPortModel outputPort2 = variable.OutputPort;
            GraphModel.CreateEdge(binary1.Input0, outputPort2);
            IPortModel outputPort3 = variable.OutputPort;
            GraphModel.CreateEdge(binary1.Input1, outputPort3);

            TestPrereqActionPostreq(mode,
                () =>
                {
                    RefreshReference(ref variable);
                    RefreshReference(ref binary0);
                    RefreshReference(ref binary1);
                    Assert.That(GetNodeCount(), Is.EqualTo(3));
                    Assert.That(GetEdgeCount(), Is.EqualTo(4));
                    Assert.That(GetVariableDeclarationCount(), Is.EqualTo(1));
                    Assert.That(GetAllNodes().OfType<VariableNodeModel>().Count(), Is.EqualTo(1));
                    Assert.That(variable.OutputPort, Is.ConnectedTo(binary0.Input0));
                    Assert.That(variable.OutputPort, Is.ConnectedTo(binary0.Input1));
                    Assert.That(variable.OutputPort, Is.ConnectedTo(binary0.Input0));
                    Assert.That(variable.OutputPort, Is.ConnectedTo(binary1.Input1));
                    return new ItemizeNodeAction(variable);
                },
                () =>
                {
                    RefreshReference(ref variable);
                    RefreshReference(ref binary0);
                    RefreshReference(ref binary1);
                    Assert.That(GetNodeCount(), Is.EqualTo(6));
                    Assert.That(GetEdgeCount(), Is.EqualTo(4));
                    Assert.That(GetAllNodes().OfType<VariableNodeModel>().Count(), Is.EqualTo(4));
                    Assert.That(GetVariableDeclarationCount(), Is.EqualTo(1));
                    Assert.That(variable.OutputPort, Is.ConnectedTo(binary0.Input0));
                    Assert.That(variable.OutputPort, Is.Not.ConnectedTo(binary0.Input1));
                    Assert.That(variable.OutputPort, Is.Not.ConnectedTo(binary1.Input0));
                    Assert.That(variable.OutputPort, Is.Not.ConnectedTo(binary1.Input1));
                });
        }

        [Test]
        public void Test_ItemizeConstantNodeAction([Values] TestingMode mode)
        {
            var constant = GraphModel.CreateConstantNode(typeof(int).GenerateTypeHandle(), "Constant", Vector2.zero);
            var binary0 = GraphModel.CreateNode<Type0FakeNodeModel>("Node0", Vector2.zero);
            var binary1 = GraphModel.CreateNode<Type0FakeNodeModel>("Node1", Vector2.zero);

            IPortModel outputPort = constant.OutputPort;
            GraphModel.CreateEdge(binary0.Input0, outputPort);
            IPortModel outputPort1 = constant.OutputPort;
            GraphModel.CreateEdge(binary0.Input1, outputPort1);
            IPortModel outputPort2 = constant.OutputPort;
            GraphModel.CreateEdge(binary1.Input0, outputPort2);
            IPortModel outputPort3 = constant.OutputPort;
            GraphModel.CreateEdge(binary1.Input1, outputPort3);

            TestPrereqActionPostreq(mode,
                () =>
                {
                    RefreshReference(ref constant);
                    RefreshReference(ref binary0);
                    RefreshReference(ref binary1);
                    Assert.That(GetNodeCount(), Is.EqualTo(3));
                    Assert.That(GetEdgeCount(), Is.EqualTo(4));
                    Assert.That(GetVariableDeclarationCount(), Is.EqualTo(0));
                    Assert.That(GetAllNodes().OfType<ConstantNodeModel>().Count(x => x.Type == typeof(int)), Is.EqualTo(1));
                    Assert.That(constant.OutputPort, Is.ConnectedTo(binary0.Input0));
                    Assert.That(constant.OutputPort, Is.ConnectedTo(binary0.Input1));
                    Assert.That(constant.OutputPort, Is.ConnectedTo(binary0.Input0));
                    Assert.That(constant.OutputPort, Is.ConnectedTo(binary1.Input1));
                    return new ItemizeNodeAction(constant);
                },
                () =>
                {
                    RefreshReference(ref constant);
                    RefreshReference(ref binary0);
                    RefreshReference(ref binary1);
                    Assert.That(GetNodeCount(), Is.EqualTo(6));
                    Assert.That(GetEdgeCount(), Is.EqualTo(4));
                    Assert.That(GetAllNodes().OfType<ConstantNodeModel>().Count(x => x.Type == typeof(int)), Is.EqualTo(4));
                    Assert.That(GetVariableDeclarationCount(), Is.EqualTo(0));
                    Assert.That(constant.OutputPort, Is.ConnectedTo(binary0.Input0));
                    Assert.That(constant.OutputPort, Is.Not.ConnectedTo(binary0.Input1));
                    Assert.That(constant.OutputPort, Is.Not.ConnectedTo(binary1.Input0));
                    Assert.That(constant.OutputPort, Is.Not.ConnectedTo(binary1.Input1));
                });
        }

        [Test]
        public void Test_ToggleLockConstantNodeAction([Values] TestingMode mode)
        {
            var constant0 = GraphModel.CreateConstantNode(typeof(int).GenerateTypeHandle(), "Constant0", Vector2.zero);
            var constant1 = GraphModel.CreateConstantNode(typeof(int).GenerateTypeHandle(), "Constant1", Vector2.zero);
            var constant2 = GraphModel.CreateConstantNode(typeof(int).GenerateTypeHandle(), "Constant2", Vector2.zero);

            TestPrereqActionPostreq(mode,
                () =>
                {
                    RefreshReference(ref constant0);
                    RefreshReference(ref constant1);
                    RefreshReference(ref constant2);
                    Assert.That(GetNodeCount(), Is.EqualTo(3));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(constant0.IsLocked, Is.False);
                    Assert.That(constant1.IsLocked, Is.False);
                    Assert.That(constant2.IsLocked, Is.False);
                    return new ToggleLockConstantNodeAction(constant0);
                },
                () =>
                {
                    RefreshReference(ref constant0);
                    RefreshReference(ref constant1);
                    RefreshReference(ref constant2);
                    Assert.That(GetNodeCount(), Is.EqualTo(3));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(constant0.IsLocked, Is.True);
                    Assert.That(constant1.IsLocked, Is.False);
                    Assert.That(constant2.IsLocked, Is.False);
                });

            TestPrereqActionPostreq(mode,
                () =>
                {
                    RefreshReference(ref constant0);
                    RefreshReference(ref constant1);
                    RefreshReference(ref constant2);
                    Assert.That(GetNodeCount(), Is.EqualTo(3));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(constant0.IsLocked, Is.True);
                    Assert.That(constant1.IsLocked, Is.False);
                    Assert.That(constant2.IsLocked, Is.False);
                    return new ToggleLockConstantNodeAction(constant1, constant2);
                },
                () =>
                {
                    RefreshReference(ref constant0);
                    RefreshReference(ref constant1);
                    RefreshReference(ref constant2);
                    Assert.That(GetNodeCount(), Is.EqualTo(3));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(constant0.IsLocked, Is.True);
                    Assert.That(constant1.IsLocked, Is.True);
                    Assert.That(constant2.IsLocked, Is.True);
                });
        }

        [Test]
        public void Test_UpdateTypeAction([Values] TestingMode mode)
        {
            var declaration = GraphModel.CreateGraphVariableDeclaration(typeof(int).GenerateTypeHandle(), "decl0", ModifierFlags.None, true);
            GraphModel.CreateVariableNode(declaration, Vector2.zero);
            GraphModel.CreateVariableNode(declaration, Vector2.zero);

            TestPrereqActionPostreq(mode,
                () =>
                {
                    declaration = GetVariableDeclaration(0);
                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(declaration.DataType, Is.EqualTo(typeof(int).GenerateTypeHandle()));
                    return new ChangeVariableTypeAction(declaration, typeof(float).GenerateTypeHandle());
                },
                () =>
                {
                    declaration = GetVariableDeclaration(0);
                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(declaration.DataType, Is.EqualTo(typeof(float).GenerateTypeHandle()));
                });
        }

        [Test]
        public void Test_UpdateTypeAction_UpdatesVariableReferences([Values] TestingMode mode)
        {
            TypeHandle intType = typeof(int).GenerateTypeHandle();
            TypeHandle floatType = typeof(float).GenerateTypeHandle();

            var declaration = GraphModel.CreateGraphVariableDeclaration(intType, "decl0", ModifierFlags.None, true);
            GraphModel.CreateVariableNode(declaration, Vector2.zero);
            GraphModel.CreateVariableNode(declaration, Vector2.zero);

            TestPrereqActionPostreq(mode,
                () =>
                {
                    declaration = GetVariableDeclaration(0);
                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));

                    Assert.That(declaration.DataType, Is.EqualTo(intType));
                    Assert.That(declaration.InitializationModel.Type, Is.EqualTo(typeof(int)));

                    foreach (var variableNodeModel in GraphModel.NodeModels.OfType<VariableNodeModel>())
                        Assert.That(variableNodeModel.OutputPort?.DataTypeHandle, Is.EqualTo(intType));

                    return new ChangeVariableTypeAction(declaration, floatType);
                },
                () =>
                {
                    declaration = GetVariableDeclaration(0);
                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));

                    Assert.That(declaration.DataType, Is.EqualTo(floatType));
                    Assert.That(declaration.InitializationModel.Type, Is.EqualTo(typeof(float)));

                    foreach (var variableNodeModel in GraphModel.NodeModels.OfType<VariableNodeModel>())
                        Assert.That(variableNodeModel.OutputPort?.DataTypeHandle, Is.EqualTo(floatType));
                });
        }

        [Test]
        public void Test_UpdateExposedAction([Values] TestingMode mode)
        {
            var declaration = GraphModel.CreateGraphVariableDeclaration(typeof(int).GenerateTypeHandle(), "decl0", ModifierFlags.None, true);
            GraphModel.CreateVariableNode(declaration, Vector2.zero);
            GraphModel.CreateVariableNode(declaration, Vector2.zero);

            TestPrereqActionPostreq(mode,
                () =>
                {
                    declaration = GetVariableDeclaration(0);
                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(declaration.IsExposed, Is.True);
                    return new UpdateExposedAction(declaration, false);
                },
                () =>
                {
                    declaration = GetVariableDeclaration(0);
                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(declaration.IsExposed, Is.False);
                });

            TestPrereqActionPostreq(mode,
                () =>
                {
                    declaration = GetVariableDeclaration(0);
                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(declaration.IsExposed, Is.False);
                    return new UpdateExposedAction(declaration, true);
                },
                () =>
                {
                    declaration = GetVariableDeclaration(0);
                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(declaration.IsExposed, Is.True);
                });
        }

        [Test]
        public void Test_UpdateTooltipAction([Values] TestingMode mode)
        {
            var declaration = GraphModel.CreateGraphVariableDeclaration(typeof(int).GenerateTypeHandle(), "decl0", ModifierFlags.None, true) as VariableDeclarationModel;
            declaration.Tooltip = "asd";
            GraphModel.CreateVariableNode(declaration, Vector2.zero);
            GraphModel.CreateVariableNode(declaration, Vector2.zero);

            TestPrereqActionPostreq(mode,
                () =>
                {
                    declaration = GetVariableDeclaration(0);
                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(declaration.Tooltip, Is.EqualTo("asd"));
                    return new UpdateTooltipAction(declaration, "qwe");
                },
                () =>
                {
                    declaration = GetVariableDeclaration(0);
                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(declaration.Tooltip, Is.EqualTo("qwe"));
                });

            TestPrereqActionPostreq(mode,
                () =>
                {
                    declaration = GetVariableDeclaration(0);
                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(declaration.Tooltip, Is.EqualTo("qwe"));
                    return new UpdateTooltipAction(declaration, "asd");
                },
                () =>
                {
                    declaration = GetVariableDeclaration(0);
                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(declaration.Tooltip, Is.EqualTo("asd"));
                });
        }
    }
}
