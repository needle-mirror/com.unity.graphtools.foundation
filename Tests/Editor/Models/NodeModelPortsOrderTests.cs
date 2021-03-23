using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using NUnit.Framework;
using UnityEditor.VisualScripting.Editor;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model;
using UnityEditor.VisualScripting.Model.Stencils;
using UnityEngine;

// ReSharper disable AccessToStaticMemberViaDerivedType

namespace UnityEditor.VisualScriptingTests.Models
{
    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    class NodeModelPortsOrderTests : BaseFixture
    {
        protected override bool CreateGraphOnStartup => true;

        [Test]
        public void DefinePortsInNewOrderReusesExistingPorts()
        {
            var node = GraphModel.CreateNode<PortOrderTestNodeModel>("test", Vector2.zero);
            node.MakePortsFromNames(new List<string> { "A", "B", "C" });
            node.DefineNode();
            Assert.That(node.InputsById.Count, Is.EqualTo(3));

            var A = node.InputsById["A"];
            var B = node.InputsById["B"];
            var C = node.InputsById["C"];

            Assert.That(A, Is.Not.Null);
            Assert.That(B, Is.Not.Null);
            Assert.That(C, Is.Not.Null);

            Assert.That(node.IsSorted, Is.True);
            node.RandomizePorts();
            Assert.That(node.IsSorted, Is.False);

            node.DefineNode();
            Assert.That(node.InputsById.Count, Is.EqualTo(3));
            Assert.That(ReferenceEquals(A, node.InputsById["A"]), Is.True);
            Assert.That(ReferenceEquals(B, node.InputsById["B"]), Is.True);
            Assert.That(ReferenceEquals(C, node.InputsById["C"]), Is.True);
        }

        [Test]
        public void RemovingAndAddingPortsPreservesExistingPorts()
        {
            var node = GraphModel.CreateNode<PortOrderTestNodeModel>("test", Vector2.zero);
            node.MakePortsFromNames(new List<string> { "A", "B", "C" });
            node.DefineNode();
            Assert.That(node.InputsById.Count, Is.EqualTo(3));

            var A = node.InputsById["A"];
            var B = node.InputsById["B"];
            var C = node.InputsById["C"];

            node.MakePortsFromNames(new List<string> { "A", "D", "B" });
            node.DefineNode();
            Assert.That(node.InputsById.Count, Is.EqualTo(3));

            Assert.That(ReferenceEquals(A, node.InputsById["A"]), Is.True);
            Assert.That(ReferenceEquals(B, node.InputsById["B"]), Is.True);
            Assert.That(ReferenceEquals(C, node.InputsById["D"]), Is.False);
        }

        [Test]
        public void ShufflingPortsPreserveConnections()
        {
            var node = GraphModel.CreateNode<PortOrderTestNodeModel>("test", Vector2.zero);
            node.MakePortsFromNames(new List<string> { "A", "B", "C" });
            node.DefineNode();

            var decl = GraphModel.CreateGraphVariableDeclaration("myInt", TypeHandle.Int, true);
            var nodeA = GraphModel.CreateVariableNode(decl, Vector2.up);
            var nodeB = GraphModel.CreateVariableNode(decl, Vector2.zero);
            var nodeC = GraphModel.CreateVariableNode(decl, Vector2.down);

            GraphModel.CreateEdge(node.InputsById["A"], nodeA.OutputPort);
            GraphModel.CreateEdge(node.InputsById["B"], nodeB.OutputPort);
            GraphModel.CreateEdge(node.InputsById["C"], nodeC.OutputPort);

            Assert.That(nodeA.OutputPort, Is.ConnectedTo(node.InputsById["A"]));
            Assert.That(nodeB.OutputPort, Is.ConnectedTo(node.InputsById["B"]));
            Assert.That(nodeC.OutputPort, Is.ConnectedTo(node.InputsById["C"]));

            Assert.That(node.IsSorted, Is.True);
            node.RandomizePorts();
            Assert.That(node.IsSorted, Is.False);

            node.DefineNode();

            Assert.That(nodeA.OutputPort, Is.ConnectedTo(node.InputsById["A"]));
            Assert.That(nodeB.OutputPort, Is.ConnectedTo(node.InputsById["B"]));
            Assert.That(nodeC.OutputPort, Is.ConnectedTo(node.InputsById["C"]));
        }

        [Test]
        public void ConnectingADifferentNodePreservesConnections([Values] TestingMode mode)
        {
            var memberX = new TypeMember(Stencil.GenerateTypeHandle(typeof(Vector3)), new List<string> { nameof(Vector3.x) });
            var memberY = new TypeMember(Stencil.GenerateTypeHandle(typeof(Vector3)), new List<string> { nameof(Vector3.y) });

            {
                var iDecl = GraphModel.CreateGraphVariableDeclaration("myInt", TypeHandle.Int, true);
                var myInt = GraphModel.CreateVariableNode(iDecl, Vector2.up);

                var vDecl = GraphModel.CreateGraphVariableDeclaration("myVec", typeof(Vector3).GenerateTypeHandle(Stencil), true);
                var myVec = GraphModel.CreateVariableNode(vDecl, Vector2.left);
                var getProperty = GraphModel.CreateGetPropertyGroupNode(Vector2.zero);
                GraphModel.CreateEdge(getProperty.InstancePort, myVec.OutputPort);

                getProperty.AddMember(memberX);
                getProperty.AddMember(memberY);

                var stack = GraphModel.CreateStack("myStack", Vector2.right);
                var log1 = stack.CreateStackedNode<LogNodeModel>("log1");
                var log2 = stack.CreateStackedNode<LogNodeModel>("log2");

                GraphModel.CreateEdge(log1.InputPort, getProperty.OutputsById[memberX.GetId()]);
                GraphModel.CreateEdge(log2.InputPort, getProperty.OutputsById[memberY.GetId()]);
            }

            TestPrereqActionPostreq(mode,
                () =>
                {
                    var logStack = GetAllStacks().Single();
                    var log1 = logStack.NodeModels[0] as LogNodeModel;
                    var log2 = logStack.NodeModels[1] as LogNodeModel;
                    var myInt = GetAllNodes().OfType<VariableNodeModel>().Single(n => n.DataType == TypeHandle.Int);
                    var getProperty = GetAllNodes().OfType<GetPropertyGroupNodeModel>().Single();
                    var portX = getProperty.OutputsById[memberX.GetId()];
                    var portY = getProperty.OutputsById[memberY.GetId()];

                    Assert.That(myInt.OutputPort.Connected, Is.False);
                    Assert.That(log1.InputPort, Is.ConnectedTo(portX));
                    Assert.That(log2.InputPort, Is.ConnectedTo(portY));
                    return new CreateEdgeAction(log1.InputPort, myInt.OutputPort, new List<IEdgeModel> { GraphModel.GetEdgesConnections(log1.InputPort).Single() });
                },
                () =>
                {
                    var logStack = GetAllStacks().Single();
                    var log1 = logStack.NodeModels[0] as LogNodeModel;
                    var log2 = logStack.NodeModels[1] as LogNodeModel;
                    var myInt = GetAllNodes().OfType<VariableNodeModel>().Single(n => n.DataType == TypeHandle.Int);
                    var getProperty = GetAllNodes().OfType<GetPropertyGroupNodeModel>().Single();
                    var portX = getProperty.OutputsById[memberX.GetId()];
                    var portY = getProperty.OutputsById[memberY.GetId()];

                    Assert.That(myInt.OutputPort.Connected, Is.True);
                    Assert.That(portX.Connected, Is.False);
                    Assert.That(log1.InputPort, Is.ConnectedTo(myInt.OutputPort));
                    Assert.That(log2.InputPort, Is.ConnectedTo(portY));
                });
        }
    }
}
