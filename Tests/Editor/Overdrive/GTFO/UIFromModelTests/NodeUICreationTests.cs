using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.Tests.TestModels;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GTFO.UIFromModelTests
{
    public class NodeUICreationTests
    {
        static IEnumerable<Func<INodeModel>> NodeModelCreators()
        {
            yield return () => new NodeModel();
            yield return () => new SingleInputNodeModel();
            yield return () => new SingleOutputNodeModel();
            yield return () =>
            {
                var model = new IONodeModel {InputCount = 3, OuputCount = 5};
                return model;
            };
        }

        [Test]
        [TestCaseSource(nameof(NodeModelCreators))]
        public void NodeHasExpectedParts(Func<INodeModel> nodeCreator)
        {
            var nodeModel = nodeCreator.Invoke();
            nodeModel.DefineNode();
            var node = new Node();
            node.SetupBuildAndUpdate(nodeModel, null, null);

            Assert.IsNotNull(node.Q<VisualElement>(Node.titleContainerPartName), "Title part was expected but not found");
            Assert.IsNotNull(node.Q<VisualElement>(Node.portContainerPartName), "Port Container part was expected but not found");
            if (nodeModel.GetType() == typeof(NodeModel))
            {
                Assert.IsNull(node.Q<Port>(), "No Port were expected but at least one was found");
            }
            else
            {
                Assert.IsNotNull(node.Q<Port>(), "At least one Port was expected but none was found");
            }
        }

        [Test]
        [TestCaseSource(nameof(NodeModelCreators))]
        public void CollapsibleNodeHasExpectedParts(Func<INodeModel> nodeCreator)
        {
            var nodeModel = nodeCreator.Invoke();
            nodeModel.DefineNode();
            var node = new CollapsibleInOutNode();
            node.SetupBuildAndUpdate(nodeModel, null, null);

            Assert.IsNotNull(node.Q<VisualElement>(CollapsibleInOutNode.titleIconContainerPartName), "Title part was expected but not found");
            Assert.IsNotNull(node.Q<VisualElement>(Node.portContainerPartName), "Port Container part was expected but not found");
            Assert.IsNotNull(node.Q<VisualElement>(CollapsibleInOutNode.collapseButtonPartName), "Collapsible Button part was expected but not found");

            if (nodeModel.GetType() == typeof(NodeModel))
            {
                Assert.IsNull(node.Q<Port>(), "No Port were expected but at least one was found");
            }
            else if (nodeModel.GetType() == typeof(IONodeModel))
            {
                var inputs = node.Q<VisualElement>(InOutPortContainerPart.inputPortsUssName);
                var outputs = node.Q<VisualElement>(InOutPortContainerPart.outputPortsUssName);
                Assert.IsNotNull(inputs, "Input Port Container part was expected but not found");
                Assert.IsNotNull(outputs, "Output Port Container part was expected but not found");
                Assert.IsNotNull(inputs.Q<Port>(), "At least one Input Port was expected but none were found");
                Assert.IsNotNull(outputs.Q<Port>(), "At least one Output Port was expected but none were found");
            }
            else
            {
                Assert.IsNotNull(node.Q<Port>(), "At least one Port was expected but none were found");
            }
        }

        [Test]
        [TestCaseSource(nameof(NodeModelCreators))]
        public void TokenNodeHasExpectedParts(Func<INodeModel> nodeCreator)
        {
            var nodeModel = nodeCreator.Invoke();
            nodeModel.DefineNode();
            var node = new TokenNode();
            node.SetupBuildAndUpdate(nodeModel, null, null);

            var inputs = node.Q<VisualElement>(TokenNode.inputPortContainerPartName);
            var outputs = node.Q<VisualElement>(TokenNode.outputPortContainerPartName);

            Assert.IsNotNull(node.Q<VisualElement>(TokenNode.titleIconContainerPartName), "Title part was expected but not found");

            if (nodeModel.GetType() == typeof(SingleInputNodeModel))
            {
                Assert.IsNotNull(inputs, "Input Port Container part was expected but not found");
                Assert.IsNull(outputs, "Output Port Container part was not expected but was found");
                Assert.IsNotNull(inputs.Q<Port>(), "At least one Port was expected but none were found");
            }
            else if (nodeModel.GetType() == typeof(SingleOutputNodeModel))
            {
                Assert.IsNull(inputs, "Input Port Container part was not expected but was found");
                Assert.IsNotNull(outputs, "Output Port Container part was expected but not found");
                Assert.IsNotNull(outputs.Q<Port>(), "At least one Port was expected but none were found");
            }
            else
            {
                Assert.IsNull(node.Q<Port>(), "No Port were expected but at least one was found");
            }
        }
    }
}
