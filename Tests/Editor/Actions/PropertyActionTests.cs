using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.EditorCommon.Utility;
using UnityEditor.VisualScripting.Editor;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model;
using UnityEditor.VisualScripting.Model.Stencils;
using UnityEngine;

// ReSharper disable AccessToStaticMemberViaDerivedType

namespace UnityEditor.VisualScriptingTests.Actions
{
    [Category("Properties")]
    [Category("Action")]
    class PropertiesActionTest : BaseFixture
    {
        protected override bool CreateGraphOnStartup => true;
        protected override Type CreatedGraphType => typeof(ClassStencil);

        [Test]
        public void Test_DisconnectSetPropertyKeepsPortType([Values] TestingMode mode)
        {
            VariableDeclarationModel trDecl = GraphModel.CreateGraphVariableDeclaration("tr", typeof(Transform).GenerateTypeHandle(Stencil), true);
            IVariableModel trVar = GraphModel.CreateVariableNode(trDecl, Vector2.left);

            FunctionModel method = GraphModel.CreateFunction("TestFunction", Vector2.zero);

            SetPropertyGroupNodeModel setters = method.CreateSetPropertyGroupNode(0);
            GraphModel.CreateEdge(setters.InstancePort, trVar.OutputPort);

            setters.AddMember(
                new TypeMember
                {
                    Path = new List<string> { "position" },
                    Type = Stencil.GenerateTypeHandle(typeof(Vector3))
                }
            );

            IConstantNodeModel constToken = GraphModel.CreateConstantNode("v3", typeof(Vector3).GenerateTypeHandle(Stencil), Vector2.left * 200);
            IEdgeModel edge = GraphModel.CreateEdge(setters.GetPortsForMembers().First(), constToken.OutputPort);

            TestPrereqActionPostreq(mode,
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(3));
                    Assert.That(GetEdgeCount(), Is.EqualTo(2));
                    Assert.That(method.NodeModels.Count(), Is.EqualTo(1));
                    Assert.That(setters.GetPortsForMembers().First().DataType.Resolve(Stencil), Is.EqualTo(typeof(Vector3)));
                    return new DeleteElementsAction(edge);
                },
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(3));
                    Assert.That(GetEdgeCount(), Is.EqualTo(1));
                    Assert.That(method.NodeModels.Count(), Is.EqualTo(1));
                    Assert.That(GetStackedNode(0, 0), Is.TypeOf<SetPropertyGroupNodeModel>());
                    Assert.That(setters.GetPortsForMembers().First().DataType.Resolve(Stencil), Is.EqualTo(typeof(Vector3)));
                });
        }

        [Test]
        public void Test_RemoveSetPropertyMemberKeepsEdges([Values] TestingMode mode)
        {
            // test that removing a SetProperty member doesn't break the edge with it's Instance
            // i.e. here: check that removing Position keeps the link with Transform

            //               |_TestFunction_____|
            // (Transform)---+-o Set Property   |
            //               | o   Position     |

            VariableDeclarationModel trDecl = GraphModel.CreateGraphVariableDeclaration("tr", typeof(Transform).GenerateTypeHandle(Stencil), true);
            IVariableModel trVar = GraphModel.CreateVariableNode(trDecl, Vector2.left);

            FunctionModel method = GraphModel.CreateFunction("TestFunction", Vector2.zero);

            SetPropertyGroupNodeModel setters = method.CreateSetPropertyGroupNode(0);
            GraphModel.CreateEdge(setters.InstancePort, trVar.OutputPort);

            PropertyInfo propertyToAdd = typeof(Transform).GetProperty("position");

            var newMember = new TypeMember
            {
                Path = new List<string> {propertyToAdd?.Name},
                Type = propertyToAdd.GetUnderlyingType().GenerateTypeHandle(Stencil)
            };

            setters.AddMember(newMember);

            TestPrereqActionPostreq(mode,
                () =>
                {
                    RefreshReference(ref method);
                    RefreshReference(ref setters);
                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GetEdgeCount(), Is.EqualTo(1));
                    Assert.That(method.NodeModels.Count(), Is.EqualTo(1));
                    Assert.That(setters.InputsByDisplayOrder.Count, Is.EqualTo(2));
                    Assert.That(setters.Members.Count, Is.EqualTo(1));
                    return new EditPropertyGroupNodeAction(
                        EditPropertyGroupNodeAction.EditType.Remove, setters, newMember);
                },
                () =>
                {
                    RefreshReference(ref method);
                    RefreshReference(ref setters);
                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GetEdgeCount(), Is.EqualTo(1));
                    Assert.That(method.NodeModels.Count(), Is.EqualTo(1));
                    Assert.That(GetStackedNode(0, 0), Is.TypeOf<SetPropertyGroupNodeModel>());
                    Assert.That(setters.InputsByDisplayOrder.Count, Is.EqualTo(1));
                    Assert.That(setters.Members.Count, Is.EqualTo(0));
                });
        }

        [TestCase(TestingMode.Action, 0, typeof(Unknown))]
        [TestCase(TestingMode.UndoRedo, 0, typeof(Unknown))]
        [TestCase(TestingMode.Action, 1, typeof(Transform))]
        [TestCase(TestingMode.UndoRedo, 1, typeof(Transform))]
        public void Test_DisconnectSetPropertyInstanceSetsRightPortType(TestingMode mode, int inputIndex, Type expectedPortType)
        {
            VariableDeclarationModel goDecl = GraphModel.CreateGraphVariableDeclaration("go", typeof(GameObject).GenerateTypeHandle(Stencil), true);
            IVariableModel goVar = GraphModel.CreateVariableNode(goDecl, Vector2.left);

            FunctionModel method = GraphModel.CreateFunction("TestFunction", Vector2.zero);

            SetPropertyGroupNodeModel setters = method.CreateSetPropertyGroupNode(0);
            IEdgeModel edge = GraphModel.CreateEdge(setters.InstancePort, goVar.OutputPort);

            PropertyInfo propertyInfo = typeof(Transform).GetProperty("position");

            var newMember = new TypeMember
            {
                Path = new List<string> {propertyInfo?.Name},
                Type = typeof(Transform).GenerateTypeHandle(Stencil)
            };

            setters.AddMember(newMember);

            TestPrereqActionPostreq(mode,
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GetEdgeCount(), Is.EqualTo(1));
                    Assert.That(method.NodeModels.Count(), Is.EqualTo(1));
                    Assert.That(setters.GetPortsForMembers().First().DataType, Is.EqualTo(typeof(Transform).GenerateTypeHandle(Stencil)));
                    return new DeleteElementsAction(edge);
                },
                () =>
                {
                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GetEdgeCount(), Is.EqualTo(0));
                    Assert.That(method.NodeModels.Count(), Is.EqualTo(1));
                    Assert.That(GetStackedNode(0, 0), Is.TypeOf<SetPropertyGroupNodeModel>());
                    Assert.That(setters.InstancePort.DataType, Is.EqualTo(TypeHandle.Unknown));
                    Assert.That(setters.GetPortsForMembers().First().DataType, Is.EqualTo(typeof(Transform).GenerateTypeHandle(Stencil)));
                });
        }

        [Test]
        public void Test_EditPropertyGroupNodeAction_AddRemove([Values] TestingMode mode)
        {
            IConstantNodeModel constant = GraphModel.CreateConstantNode("toto", typeof(Vector3).GenerateTypeHandle(Stencil), Vector2.zero);
            GetPropertyGroupNodeModel property = GraphModel.CreateGetPropertyGroupNode(Vector2.zero);
            GraphModel.CreateEdge(property.InstancePort, constant.OutputPort);

            Type type = typeof(GameObject);
            MemberInfo memberInfo = type.GetMembers()[0];
            var newMember = new TypeMember
            {
                Path = new List<string> {memberInfo.Name},
                Type = memberInfo.GetUnderlyingType().GenerateTypeHandle(Stencil)
            };


            TestPrereqActionPostreq(mode,
                () =>
                {
                    property = (GetPropertyGroupNodeModel)GraphModel.NodesByGuid[property.Guid];

                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GetEdgeCount(), Is.EqualTo(1));
                    Assert.That(property.GetPortsForMembers().Count(), Is.EqualTo(0));
                    Assert.That(property.OutputsByDisplayOrder.Count(), Is.EqualTo(0));
                    return new EditPropertyGroupNodeAction(
                        EditPropertyGroupNodeAction.EditType.Add,
                        property,
                        newMember);
                },
                () =>
                {
                    property = (GetPropertyGroupNodeModel)GraphModel.NodesByGuid[property.Guid];

                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GetEdgeCount(), Is.EqualTo(1));
                    Assert.That(property.GetPortsForMembers().Count(), Is.EqualTo(1));
                    Assert.That(property.OutputsByDisplayOrder.Count(), Is.EqualTo(1));
                });

            TestPrereqActionPostreq(mode,
                () =>
                {
                    property = (GetPropertyGroupNodeModel)GraphModel.NodesByGuid[property.Guid];

                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GetEdgeCount(), Is.EqualTo(1));
                    Assert.That(property.GetPortsForMembers().Count(), Is.EqualTo(1));
                    Assert.That(property.OutputsByDisplayOrder.Count(), Is.EqualTo(1));
                    return new EditPropertyGroupNodeAction(
                        EditPropertyGroupNodeAction.EditType.Remove,
                        property,
                        newMember);
                },
                () =>
                {
                    property = (GetPropertyGroupNodeModel)GraphModel.NodesByGuid[property.Guid];

                    Assert.That(GetNodeCount(), Is.EqualTo(2));
                    Assert.That(GetEdgeCount(), Is.EqualTo(1));
                    Assert.That(property.GetPortsForMembers().Count(), Is.EqualTo(0));
                    Assert.That(property.OutputsByDisplayOrder.Count(), Is.EqualTo(0));
                });
        }
    }
}
