using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Moq;
using NUnit.Framework;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model;
using UnityEditor.VisualScripting.Model.Stencils;
using UnityEngine;
using PortName = UnityEditor.VisualScripting.Model.BinaryOperatorNodeModel.PortName;

namespace UnityEditor.VisualScriptingTests.Models
{
    class BinaryOperatorNodeModelTests : BaseFixture
    {
        protected override bool CreateGraphOnStartup => true;

        static BinaryOperatorNodeModel CreateNode(IGraphModel graphModel, BinaryOperatorKind kind,
            Func<INodeModel, IPortModel> makePortA, Func<INodeModel, IPortModel> makePortB)
        {
            var model = new Mock<BinaryOperatorNodeModel>();
            model.Object.AssetModel = graphModel.AssetModel;
            model.Object.Kind = kind;
            model.Setup(p => p.InputPortA).Returns(makePortA.Invoke(model.Object));
            model.Setup(p => p.InputPortB).Returns(makePortB.Invoke(model.Object));

            return model.Object;
        }

        static IPortModel CreateInputPort(Stencil stencil, INodeModel model, Type type, bool connected)
        {
            var port = new Mock<IPortModel>();
            port.Setup(p => p.DataType).Returns(stencil.GenerateTypeHandle(type));
            port.Setup(p => p.Connected).Returns(connected);
            port.Setup(p => p.NodeModel).Returns(model);

            return port.Object;
        }

        [UsedImplicitly]
        static IEnumerable<TestCaseData> HasValidOperationForInputTestCaseData
        {
            get
            {
                Func<IGraphModel, BinaryOperatorNodeModel> createNodeModel = graphModel => CreateNode(graphModel, BinaryOperatorKind.Multiply,
                    node => CreateInputPort(graphModel.Stencil, node, typeof(Unknown), false),
                    node => CreateInputPort(graphModel.Stencil, node, typeof(Unknown), false));
                yield return new TestCaseData(typeof(float), createNodeModel, PortName.PortA, true)
                    .SetName("Multiply - Inputs(Unknown, Unknown) - Var(float) -> Match");

                createNodeModel = graphModel => CreateNode(graphModel, BinaryOperatorKind.Divide,
                    node => CreateInputPort(graphModel.Stencil, node, typeof(float), true),
                    node => CreateInputPort(graphModel.Stencil, node, typeof(Unknown), false));
                yield return new TestCaseData(typeof(int), createNodeModel, PortName.PortB, true)
                    .SetName("Divide - Inputs(float, Unknown) - Var(int) -> Match");
                yield return new TestCaseData(typeof(Vector2), createNodeModel, PortName.PortB, false)
                    .SetName("Divide - Inputs(float, Unknown) - Var(Vector2) -> Do not Match");

                createNodeModel = graphModel => CreateNode(graphModel, BinaryOperatorKind.Divide,
                    node => CreateInputPort(graphModel.Stencil, node, typeof(Unknown), true),
                    node => CreateInputPort(graphModel.Stencil, node, typeof(float), false));
                yield return new TestCaseData(typeof(Vector2), createNodeModel, PortName.PortA, true)
                    .SetName("Divide - Inputs(Unknown, float) - Var(Vector2) -> Match");
            }
        }

        [TestCaseSource(nameof(HasValidOperationForInputTestCaseData))]
        public void TestHasValidOperationForInput(Type dataType,
            Func<IGraphModel, BinaryOperatorNodeModel> createBinaryNodeModel, PortName portName, bool result)
        {
            var nodeModel = createBinaryNodeModel.Invoke(GraphModel);

            Assert.AreEqual(result, nodeModel.HasValidOperationForInput(nodeModel.GetPort(portName), Stencil.GenerateTypeHandle(dataType)));
        }
    }
}
