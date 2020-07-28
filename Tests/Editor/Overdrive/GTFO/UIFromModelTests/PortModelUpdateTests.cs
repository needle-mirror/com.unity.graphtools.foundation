using System;
using System.Collections.Generic;
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

        class FakePortModel : PortModel
        {
            public bool FakeIsConnected { get; set; }

            public override IEnumerable<IGTFEdgeModel> GetConnectedEdges()
            {
                if (FakeIsConnected)
                {
                    return new[] { new BasicModel.EdgeModel() };
                }
                return Enumerable.Empty<IGTFEdgeModel>();
            }
        }

        class FakeIONodeModel : IONodeModel
        {
            public new void CreatePorts(int inputPorts, int outputPorts)
            {
                m_InputPorts.Clear();
                for (var i = 0; i < inputPorts; i++)
                {
                    m_InputPorts.Add(new FakePortModel
                    {
                        Direction = Direction.Input,
                        NodeModel = this,
                        GraphModel = GraphModel
                    });
                }

                m_OutputPorts.Clear();
                for (var i = 0; i < outputPorts; i++)
                {
                    m_OutputPorts.Add(new FakePortModel
                    {
                        Direction = Direction.Output,
                        NodeModel = this,
                        GraphModel = GraphModel
                    });
                }
            }
        }

        [Test]
        public void ConnectingPortUpdateClasses()
        {
            var nodeModel = new FakeIONodeModel();
            nodeModel.CreatePorts(1, 1);
            var node = new CollapsibleInOutNode();
            node.SetupBuildAndUpdate(nodeModel, null, null);

            var portModel = nodeModel.Ports.First() as FakePortModel;
            Assert.IsNotNull(portModel);
            var port = portModel.GetUI<Port>(null);
            Assert.IsNotNull(port);

            Assert.IsTrue(port.ClassListContains(Port.k_NotConnectedModifierUssClassName));
            Assert.IsFalse(port.ClassListContains(Port.k_ConnectedModifierUssClassName));

            portModel.FakeIsConnected = true;
            port.UpdateFromModel();

            Assert.IsFalse(port.ClassListContains(Port.k_NotConnectedModifierUssClassName));
            Assert.IsTrue(port.ClassListContains(Port.k_ConnectedModifierUssClassName));
        }
    }
}
