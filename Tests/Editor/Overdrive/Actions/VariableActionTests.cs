using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEditor.GraphToolsFoundation.Overdrive.GraphElements;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting;
using UnityEngine;
using IRenamable = UnityEditor.GraphToolsFoundation.Overdrive.Model.IRenamable;
using Object = UnityEngine.Object;

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
        public void Test_DuplicateGraphVariableDeclarationsAction([Values] TestingMode mode)
        {
            var declaration0 = GraphModel.CreateGraphVariableDeclaration("decl0", typeof(int).GenerateTypeHandle(), ModifierFlags.None, true);
            var declaration1 = GraphModel.CreateGraphVariableDeclaration("decl1", typeof(int).GenerateTypeHandle(), ModifierFlags.None, true);

            TestPrereqActionPostreq(mode,
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(0));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(GetVariableDeclarationCount(), Is.EqualTo(2));
                    return new DuplicateGraphVariableDeclarationsAction(new List<IGTFVariableDeclarationModel> { declaration0, declaration1 });
                },
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(0));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(GetVariableDeclarationCount(), Is.EqualTo(4));
                });
        }

        [Test]
        public void Test_CreateConstantNodeAction([Values] TestingMode mode)
        {
            Tuple<Type, Type>[] constants =
            {
                new Tuple<Type, Type>(typeof(bool),       typeof(BooleanConstant)),
                new Tuple<Type, Type>(typeof(Color),      typeof(ColorConstant)),
                new Tuple<Type, Type>(typeof(int),        typeof(IntConstant)),
                new Tuple<Type, Type>(typeof(float),      typeof(FloatConstant)),
                new Tuple<Type, Type>(typeof(double),     typeof(DoubleConstant)),
                new Tuple<Type, Type>(typeof(InputName),  typeof(InputConstant)),
                new Tuple<Type, Type>(typeof(Quaternion), typeof(QuaternionConstant)),
                new Tuple<Type, Type>(typeof(string),     typeof(StringConstant)),
                new Tuple<Type, Type>(typeof(Vector2),    typeof(Vector2Constant)),
                new Tuple<Type, Type>(typeof(Vector3),    typeof(Vector3Constant)),
                new Tuple<Type, Type>(typeof(Vector4),    typeof(Vector4Constant)),
            };

            for (var i = 0; i < constants.Length; i++)
            {
                var iCopy = i;
                TestPrereqActionPostreq(mode,
                    () =>
                    {
                        var constant = constants[iCopy];
                        Assert.That(GetNodeCount(), Is.EqualTo(iCopy));
                        Assert.That(GetEdgeCount(), Is.EqualTo(0));
                        return new CreateConstantNodeAction("toto", constant.Item1.GenerateTypeHandle(), Vector2.zero);
                    },
                    () =>
                    {
                        var constant = constants[iCopy];
                        Assert.That(GetNodeCount(), Is.EqualTo(iCopy + 1));
                        Assert.That(GetEdgeCount(), Is.EqualTo(0));
                        Assert.That(GetNode(iCopy), Is.TypeOf<ConstantNodeModel>());
                        Assert.That(((ConstantNodeModel)GetNode(iCopy)).Value, Is.TypeOf(constant.Item2));
                        Assert.That(((ConstantNodeModel)GetNode(iCopy)).Type, Is.EqualTo(constant.Item1));
                    });
            }
        }

        [Test]
        public void Test_CreateVariableNodeAction([Values] TestingMode mode)
        {
            var declaration = GraphModel.CreateGraphVariableDeclaration("decl0", typeof(int).GenerateTypeHandle(), ModifierFlags.None, true);

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
            var declaration0 = GraphModel.CreateGraphVariableDeclaration("decl0", typeof(int).GenerateTypeHandle(), ModifierFlags.None, true);
            var declaration1 = GraphModel.CreateGraphVariableDeclaration("decl1", typeof(int).GenerateTypeHandle(), ModifierFlags.None, true);

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
                    return new DeleteElementsAction(declaration0);
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
                    return new DeleteElementsAction(declaration1);
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
            var variable = GraphModel.CreateGraphVariableDeclaration("toto", typeof(int).GenerateTypeHandle(), ModifierFlags.None, true);

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
        public void Test_ReorderGraphVariableDeclarationAction([Values] TestingMode mode)
        {
            var declaration0 = GraphModel.CreateGraphVariableDeclaration("decl0", typeof(int).GenerateTypeHandle(), ModifierFlags.None, true);
            var declaration1 = GraphModel.CreateGraphVariableDeclaration("decl1", typeof(int).GenerateTypeHandle(), ModifierFlags.None, true);
            var declaration2 = GraphModel.CreateGraphVariableDeclaration("decl2", typeof(int).GenerateTypeHandle(), ModifierFlags.None, true);
            var declaration3 = GraphModel.CreateGraphVariableDeclaration("decl3", typeof(int).GenerateTypeHandle(), ModifierFlags.None, true);

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
                    return new ReorderGraphVariableDeclarationAction(declaration0, 3);
                },
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(0));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(GetVariableDeclarationCount(), Is.EqualTo(4));
                    Assert.That(GetVariableDeclaration(0).Guid, Is.EqualTo(declaration1.Guid));
                    Assert.That(GetVariableDeclaration(1).Guid, Is.EqualTo(declaration2.Guid));
                    Assert.That(GetVariableDeclaration(2).Guid, Is.EqualTo(declaration0.Guid));
                    Assert.That(GetVariableDeclaration(3).Guid, Is.EqualTo(declaration3.Guid));
                });
        }

        [Test]
        public void Test_ConvertVariableNodeToConstantNodeAction([Values] TestingMode mode)
        {
            var declaration0 = GraphModel.CreateGraphVariableDeclaration("decl0", typeof(int).GenerateTypeHandle(), ModifierFlags.None, true);
            var node0 = GraphModel.CreateNode<Type0FakeNodeModel>("Node0", Vector2.zero);
            var node1 = GraphModel.CreateVariableNode(declaration0, Vector2.zero);
            IGTFPortModel outputPort = node1.OutputPort;
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
                });
        }

        [Test]
        public void Test_ConvertConstantNodeToVariableNodeAction([Values] TestingMode mode)
        {
            var binary = GraphModel.CreateNode<Type0FakeNodeModel>("Node0", Vector2.zero);
            var constant = GraphModel.CreateConstantNode("const0", typeof(int).GenerateTypeHandle(), Vector2.zero);
            IGTFPortModel outputPort = constant.OutputPort;
            GraphModel.CreateEdge(binary.Input0, outputPort);

            TestPrereqActionPostreq(mode,
                () =>
                {
                    var c = GraphModel.NodeModels.OfType<IGTFConstantNodeModel>().First();
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
                    Assert.That(n1.DataType, Is.EqualTo(typeof(int).GenerateTypeHandle()));
                });
        }

        [Test]
        public void Test_ItemizeVariableNodeAction([Values] TestingMode mode)
        {
            var declaration = GraphModel.CreateGraphVariableDeclaration("decl0", typeof(int).GenerateTypeHandle(), ModifierFlags.None, true);
            var variable = GraphModel.CreateVariableNode(declaration, Vector2.zero);
            var binary0 = GraphModel.CreateNode<Type0FakeNodeModel>("Node0", Vector2.zero);
            var binary1 = GraphModel.CreateNode<Type0FakeNodeModel>("Node1", Vector2.zero);

            IGTFPortModel outputPort = variable.OutputPort;
            GraphModel.CreateEdge(binary0.Input0, outputPort);
            IGTFPortModel outputPort1 = variable.OutputPort;
            GraphModel.CreateEdge(binary0.Input1, outputPort1);
            IGTFPortModel outputPort2 = variable.OutputPort;
            GraphModel.CreateEdge(binary1.Input0, outputPort2);
            IGTFPortModel outputPort3 = variable.OutputPort;
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
                    return new ItemizeVariableNodeAction(variable);
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
            var constant = GraphModel.CreateConstantNode("Constant", typeof(int).GenerateTypeHandle(), Vector2.zero);
            var binary0 = GraphModel.CreateNode<Type0FakeNodeModel>("Node0", Vector2.zero);
            var binary1 = GraphModel.CreateNode<Type0FakeNodeModel>("Node1", Vector2.zero);

            IGTFPortModel outputPort = constant.OutputPort;
            GraphModel.CreateEdge(binary0.Input0, outputPort);
            IGTFPortModel outputPort1 = constant.OutputPort;
            GraphModel.CreateEdge(binary0.Input1, outputPort1);
            IGTFPortModel outputPort2 = constant.OutputPort;
            GraphModel.CreateEdge(binary1.Input0, outputPort2);
            IGTFPortModel outputPort3 = constant.OutputPort;
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
                    return new ItemizeConstantNodeAction(constant);
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
            var constant0 = GraphModel.CreateConstantNode("Constant0", typeof(int).GenerateTypeHandle(), Vector2.zero);
            var constant1 = GraphModel.CreateConstantNode("Constant1", typeof(int).GenerateTypeHandle(), Vector2.zero);
            var constant2 = GraphModel.CreateConstantNode("Constant2", typeof(int).GenerateTypeHandle(), Vector2.zero);

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
            var declaration = GraphModel.CreateGraphVariableDeclaration("decl0", typeof(int).GenerateTypeHandle(), ModifierFlags.None, true);
            GraphModel.CreateVariableNode(declaration, Vector2.zero);
            GraphModel.CreateVariableNode(declaration, Vector2.zero);

            TestPrereqActionPostreq(mode,
                () =>
                {
                    declaration = GetVariableDeclaration(0);
                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(declaration.DataType, Is.EqualTo(typeof(int).GenerateTypeHandle()));
                    return new UpdateTypeAction(declaration, typeof(float).GenerateTypeHandle());
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

            var declaration = GraphModel.CreateGraphVariableDeclaration("decl0", intType, ModifierFlags.None, true);
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

                    return new UpdateTypeAction(declaration, floatType);
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
            var declaration = GraphModel.CreateGraphVariableDeclaration("decl0", typeof(int).GenerateTypeHandle(), ModifierFlags.None, true);
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
            var declaration = GraphModel.CreateGraphVariableDeclaration("decl0", typeof(int).GenerateTypeHandle(), ModifierFlags.None, true) as VariableDeclarationModel;
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
