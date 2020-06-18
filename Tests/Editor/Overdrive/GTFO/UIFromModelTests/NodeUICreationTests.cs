using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.GraphElements;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GTFO.UIFromModelTests
{
    public class NodeUICreationTests
    {
        static IEnumerable<Func<IGTFNodeModel>> NodeModelCreators()
        {
            yield return () => new NodeModel();
            yield return () => new SingleInputNodeModel();
            yield return () => new SingleOutputNodeModel();
            yield return () =>
            {
                var model = new IONodeModel();
                model.CreatePorts(3, 5);
                return model;
            };
        }

        [Test]
        [TestCaseSource(nameof(NodeModelCreators))]
        public void NodeHasExpectedParts(Func<IGTFNodeModel> nodeCreator)
        {
            var nodeModel = nodeCreator.Invoke();
            var node = new Node();
            node.SetupBuildAndUpdate(nodeModel, null, null);

            Assert.IsNotNull(node.Q<VisualElement>(Node.k_TitleContainerPartName));
            Assert.IsNotNull(node.Q<VisualElement>(Node.k_PortContainerPartName));
            if (nodeModel.GetType() == typeof(NodeModel))
            {
                Assert.IsNull(node.Q<Port>());
            }
            else
            {
                Assert.IsNotNull(node.Q<Port>());
            }
        }

        [Test]
        [TestCaseSource(nameof(NodeModelCreators))]
        public void CollapsibleNodeHasExpectedParts(Func<IGTFNodeModel> nodeCreator)
        {
            var nodeModel = nodeCreator.Invoke();
            var node = new CollapsibleInOutNode();
            node.SetupBuildAndUpdate(nodeModel, null, null);

            Assert.IsNotNull(node.Q<VisualElement>(Node.k_TitleContainerPartName));
            Assert.IsNotNull(node.Q<VisualElement>(Node.k_PortContainerPartName));
            Assert.IsNotNull(node.Q<VisualElement>(CollapsibleInOutNode.k_CollapseButtonPartName));

            if (nodeModel.GetType() == typeof(NodeModel))
            {
                Assert.IsNull(node.Q<Port>());
            }
            else if (nodeModel.GetType() == typeof(IONodeModel))
            {
                var inputs = node.Q<VisualElement>(InOutPortContainerPart.k_InputPortsUssName);
                var outputs = node.Q<VisualElement>(InOutPortContainerPart.k_OutputPortsUssName);
                Assert.IsNotNull(inputs);
                Assert.IsNotNull(outputs);
                Assert.IsNotNull(inputs.Q<Port>());
                Assert.IsNotNull(outputs.Q<Port>());
            }
            else
            {
                Assert.IsNotNull(node.Q<Port>());
            }
        }

        [Test]
        [TestCaseSource(nameof(NodeModelCreators))]
        public void TokenNodeHasExpectedParts(Func<IGTFNodeModel> nodeCreator)
        {
            var nodeModel = nodeCreator.Invoke();
            var node = new TokenNode();
            node.SetupBuildAndUpdate(nodeModel, null, null);

            var inputs = node.Q<VisualElement>(TokenNode.k_InputPortContainerPartName);
            var outputs = node.Q<VisualElement>(TokenNode.k_OutputPortContainerPartName);

            Assert.IsNotNull(node.Q<VisualElement>(Node.k_TitleContainerPartName));

            if (nodeModel.GetType() == typeof(SingleInputNodeModel))
            {
                Assert.IsNotNull(inputs);
                Assert.IsNull(outputs);
                Assert.IsNotNull(inputs.Q<Port>());
            }
            else if (nodeModel.GetType() == typeof(SingleOutputNodeModel))
            {
                Assert.IsNull(inputs);
                Assert.IsNotNull(outputs);
                Assert.IsNotNull(outputs.Q<Port>());
            }
            else
            {
                Assert.IsNull(node.Q<Port>());
            }
        }
    }
}
