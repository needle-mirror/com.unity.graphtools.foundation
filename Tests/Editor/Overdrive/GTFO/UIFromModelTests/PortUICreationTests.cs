using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.GraphElements;
using UnityEditor.GraphToolsFoundation.Overdrive.Tests.GTFO.Helpers;
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
            var model = new IONodeModel();
            model.CreatePorts(1, 0);
            var node = new Node();
            node.SetupBuildAndUpdate(model, null, null);

            var portModel = model.InputPorts.First();
            Assert.IsNotNull(portModel);

            var port = portModel.GetUI<Port>(null);
            Assert.IsNotNull(port);
            Assert.IsTrue(port.ClassListContains(Port.k_InputModifierUssClassName));
            Assert.IsTrue(port.ClassListContains(Port.k_NotConnectedModifierUssClassName));
            Assert.IsFalse(port.ClassListContains(Port.k_ConnectedModifierUssClassName));
            Assert.IsTrue(port.ClassListContains(Port.k_PortDataTypeClassNamePrefix + portModel.PortDataType.Name.ToKebabCase()));
            Assert.IsNotNull(port.Q<VisualElement>(Port.k_ConnectorPartName));
        }

        [Test]
        public void OutputPortHasExpectedParts()
        {
            var model = new IONodeModel();
            model.CreatePorts(0, 1);
            var node = new Node();
            node.SetupBuildAndUpdate(model, null, null);

            var portModel = model.OutputPorts.First();
            Assert.IsNotNull(portModel);

            var port = portModel.GetUI<Port>(null);
            Assert.IsNotNull(port);
            Assert.IsTrue(port.ClassListContains(Port.k_OutputModifierUssClassName));
            Assert.IsTrue(port.ClassListContains(Port.k_NotConnectedModifierUssClassName));
            Assert.IsFalse(port.ClassListContains(Port.k_ConnectedModifierUssClassName));
            Assert.IsTrue(port.ClassListContains(Port.k_PortDataTypeClassNamePrefix + portModel.PortDataType.Name.ToKebabCase()));
            Assert.IsNotNull(port.Q<VisualElement>(Port.k_ConnectorPartName));
        }
    }

    class PortUICreationTestsNeedingRepaint : BaseTestFixture
    {
        GraphView m_GraphView;
        Store m_Store;
        GraphModel m_GraphModel;

        [SetUp]
        public new void SetUp()
        {
            m_GraphModel = new GraphModel();
            m_Store = new Store(new Helpers.TestState(m_GraphModel), StoreHelper.RegisterReducers);
            m_GraphView = new TestGraphView(m_Store);

            m_GraphView.name = "theView";
            m_GraphView.viewDataKey = "theView";
            m_GraphView.StretchToParentSize();

            m_Window.rootVisualElement.Add(m_GraphView);
        }

        [UnityTest]
        public IEnumerator PortConnectorAndCapHavePortColor()
        {
            var nodeModel = new IONodeModel();
            nodeModel.CreatePorts(0, 1);
            var node = new CollapsibleInOutNode();
            node.SetupBuildAndUpdate(nodeModel, m_Store, m_GraphView);
            m_GraphView.AddElement(node);
            yield return null;

            var portModel = nodeModel.Ports.First();
            var port = portModel.GetUI<Port>(m_GraphView);
            Assert.IsNotNull(port);
            var connector = port.Q(PortConnectorPart.k_ConnectorUssName);
            var connectorCap = port.Q(PortConnectorPart.k_ConnectorCapUssName);

            CustomStyleProperty<Color> portColorProperty = new CustomStyleProperty<Color>("--port-color");
            Color portColor;
            Assert.IsTrue(port.customStyle.TryGetValue(portColorProperty, out portColor));

            Assert.AreEqual(portColor, connector.resolvedStyle.borderBottomColor);
            Assert.AreEqual(portColor, connectorCap.resolvedStyle.backgroundColor);
        }
    }
}
