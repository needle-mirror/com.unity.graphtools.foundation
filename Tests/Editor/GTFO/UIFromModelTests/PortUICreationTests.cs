using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.Tests.TestModels;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GTFO.UIFromModelTests
{
    public class PortUICreationTests
    {
        [Test]
        public void InputPortHasExpectedParts()
        {
            GraphView graphView = new GraphView(null, null, "");
            var model = new SingleInputNodeModel();
            model.DefineNode();
            var node = new Node();
            node.SetupBuildAndUpdate(model, null, graphView);

            var portModel = model.GetInputPorts().First();
            Assert.IsNotNull(portModel);

            var port = portModel.GetUI<Port>(graphView);
            Assert.IsNotNull(port);
            Assert.IsTrue(port.ClassListContains(Port.inputModifierUssClassName));
            Assert.IsTrue(port.ClassListContains(Port.notConnectedModifierUssClassName));
            Assert.IsFalse(port.ClassListContains(Port.connectedModifierUssClassName));
            Assert.IsTrue(port.ClassListContains(Port.portDataTypeClassNamePrefix + portModel.PortDataType.Name.ToKebabCase()));
            Assert.IsNotNull(port.SafeQ<VisualElement>(Port.connectorPartName));
        }

        [Test]
        public void OutputPortHasExpectedParts()
        {
            GraphView graphView = new GraphView(null, null, "");
            var model = new SingleOutputNodeModel();
            model.DefineNode();
            var node = new Node();
            node.SetupBuildAndUpdate(model, null, graphView);

            var portModel = model.GetOutputPorts().First();
            Assert.IsNotNull(portModel);

            var port = portModel.GetUI<Port>(graphView);
            Assert.IsNotNull(port);
            Assert.IsTrue(port.ClassListContains(Port.outputModifierUssClassName));
            Assert.IsTrue(port.ClassListContains(Port.notConnectedModifierUssClassName));
            Assert.IsFalse(port.ClassListContains(Port.connectedModifierUssClassName));
            Assert.IsTrue(port.ClassListContains(Port.portDataTypeClassNamePrefix + portModel.PortDataType.Name.ToKebabCase()));
            Assert.IsNotNull(port.SafeQ<VisualElement>(Port.connectorPartName));
        }
    }

    class PortUICreationTestsNeedingRepaint : BaseTestFixture
    {
        GraphView m_GraphView;
        CommandDispatcher m_CommandDispatcher;
        GraphModel m_GraphModel;

        [SetUp]
        public new void SetUp()
        {
            m_GraphModel = new GraphModel();
            m_CommandDispatcher = new CommandDispatcher(new TestGraphToolState(m_Window.GUID, m_GraphModel));
            m_GraphView = new GraphView(m_Window, m_CommandDispatcher, "theView");

            m_GraphView.viewDataKey = "theView";
            m_GraphView.StretchToParentSize();

            m_Window.rootVisualElement.Add(m_GraphView);
        }

        [TearDown]
        public new void TearDown()
        {
            m_Window.rootVisualElement.Remove(m_GraphView);
            m_GraphModel = null;
            m_CommandDispatcher = null;
            m_GraphView = null;
        }

        [UnityTest]
        public IEnumerator PortConnectorAndCapHavePortColor()
        {
            var nodeModel = new SingleOutputNodeModel();
            nodeModel.DefineNode();
            var node = new CollapsibleInOutNode();
            node.SetupBuildAndUpdate(nodeModel, m_CommandDispatcher, m_GraphView);
            m_GraphView.AddElement(node);
            yield return null;

            var portModel = nodeModel.Ports.First();
            var port = portModel.GetUI<Port>(m_GraphView);
            Assert.IsNotNull(port);
            var connector = port.SafeQ(PortConnectorPart.connectorUssName);
            var connectorCap = port.SafeQ(PortConnectorPart.connectorCapUssName);

            CustomStyleProperty<Color> portColorProperty = new CustomStyleProperty<Color>("--port-color");
            Color portColor;
            Assert.IsTrue(port.customStyle.TryGetValue(portColorProperty, out portColor));

            Assert.AreEqual(portColor, connector.resolvedStyle.borderBottomColor);
            Assert.AreEqual(portColor, connectorCap.resolvedStyle.backgroundColor);
        }
    }
}
