using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.GraphElements;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GTFO.UIFromModelTests
{
    class PortModelUpdateTests
    {
        [Test]
        public void ChangingPortNameChangesPortLabel()
        {
            var nodeModel = new IONodeModel();
            nodeModel.CreatePorts(0, 1);
            var node = new CollapsibleInOutNode();
            node.SetupBuildAndUpdate(nodeModel, null, null);

            var portModel = nodeModel.Ports.First();
            var port = portModel.GetUI<Port>(null);
            Assert.IsNotNull(port);

            var label = port.Q<Label>();
            Assert.AreEqual("", label.text);

            Assert.IsNotNull(portModel as IHasTitle);
            const string newTitle = "New Title";
            (portModel as IHasTitle).Title = newTitle;
            node.UpdateFromModel();

            Assert.AreEqual(newTitle, label.text);
        }

        [Test]
        public void ChangingTooltipChangesUITooltip()
        {
            var nodeModel = new IONodeModel();
            nodeModel.CreatePorts(0, 1);
            var node = new CollapsibleInOutNode();
            node.SetupBuildAndUpdate(nodeModel, null, null);

            var portModel = nodeModel.Ports.First();
            Assert.IsNotNull(portModel as PortModel);
            var port = portModel.GetUI<Port>(null);
            Assert.IsNotNull(port);

            Assert.AreEqual("", port.tooltip);

            const string newTooltip = "New Tooltip";
            (portModel as PortModel).ToolTip = newTooltip;
            node.UpdateFromModel();

            Assert.AreEqual(newTooltip, port.tooltip);
        }

        [Test]
        public void ConnectingPortUpdateClasses()
        {
            var nodeModel = new IONodeModel();
            nodeModel.CreatePorts(0, 1);
            var node = new CollapsibleInOutNode();
            node.SetupBuildAndUpdate(nodeModel, null, null);

            var portModel = nodeModel.Ports.First();
            var port = portModel.GetUI<Port>(null);
            Assert.IsNotNull(port);

            Assert.IsTrue(port.ClassListContains(Port.k_NotConnectedModifierUssClassName));
            Assert.IsFalse(port.ClassListContains(Port.k_ConnectedModifierUssClassName));
            (portModel as PortModel)?.FakeIsConnected(true);
            port.UpdateFromModel();
            Assert.IsFalse(port.ClassListContains(Port.k_NotConnectedModifierUssClassName));
            Assert.IsTrue(port.ClassListContains(Port.k_ConnectedModifierUssClassName));
        }
    }
}
