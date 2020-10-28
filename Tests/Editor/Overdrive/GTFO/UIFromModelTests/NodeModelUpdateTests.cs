using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.Tests.TestModels;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GTFO.UIFromModelTests
{
    class NodeModelUpdateTests
    {
        [Test]
        public void CollapsingNodeModelCollapsesNode()
        {
            var nodeModel = new IONodeModel();
            var node = new CollapsibleInOutNode();
            node.SetupBuildAndUpdate(nodeModel, null, null);

            var collapseButton = node.Q<CollapseButton>(CollapsibleInOutNode.k_CollapseButtonPartName);
            Assert.IsFalse(collapseButton.value);

            nodeModel.Collapsed = true;
            node.UpdateFromModel();
            Assert.IsTrue(collapseButton.value);
        }

        [Test]
        public void RenamingNonRenamableNodeModelUpdatesTitleLabel()
        {
            const string initialTitle = "Initial title";
            const string newTitle = "New title";

            var nodeModel = new IONodeModel { Title = initialTitle };
            var node = new CollapsibleInOutNode();
            node.SetupBuildAndUpdate(nodeModel, null, null);

            var titleLabel = node.Q<Label>(EditableTitlePart.k_TitleLabelName);
            Assert.AreEqual(initialTitle, titleLabel.text);

            nodeModel.Title = newTitle;
            node.UpdateFromModel();
            Assert.AreEqual(newTitle, titleLabel.text);
        }

        [Test]
        public void RenamingRenamableNodeModelUpdatesTitleLabel()
        {
            const string initialTitle = "Initial title";
            const string newTitle = "New title";

            var nodeModel = new NodeModel { Title = initialTitle };
            var node = new Node();
            node.SetupBuildAndUpdate(nodeModel, null, null);

            var titleLabel = node.Q(EditableTitlePart.k_TitleLabelName).Q<Label>(EditableLabel.k_LabelName);
            Assert.AreEqual(initialTitle, titleLabel.text);

            nodeModel.Title = newTitle;
            node.UpdateFromModel();
            Assert.AreEqual(newTitle, titleLabel.text);
        }

        [Test]
        public void ChangingPortsOnNodeModelUpdatesNodePort()
        {
            const int originalInputPortCount = 3;
            const int originalOutputPortCount = 2;
            var nodeModel = new IONodeModel {InputCount = originalInputPortCount, OuputCount = originalOutputPortCount};
            nodeModel.DefineNode();
            var node = new Node();
            node.SetupBuildAndUpdate(nodeModel, null, null);

            var ports = node.Query(className: "ge-port").ToList();
            Assert.AreEqual(originalInputPortCount + originalOutputPortCount, ports.Count);

            const int newInputPortCount = 1;
            const int newOutputPortCount = 3;
            nodeModel.InputCount = newInputPortCount;
            nodeModel.OuputCount = newOutputPortCount;
            nodeModel.DefineNode();
            node.UpdateFromModel();

            ports = node.Query(className: "ge-port").ToList();
            Assert.AreEqual(newInputPortCount + newOutputPortCount, ports.Count);
        }

        [Test]
        public void ChangingPortsOnNodeModelUpdatesCollapsibleInOutNodePort()
        {
            const int originalInputPortCount = 3;
            const int originalOutputPortCount = 2;
            var nodeModel = new IONodeModel {InputCount = originalInputPortCount, OuputCount = originalOutputPortCount};
            nodeModel.DefineNode();
            var node = new CollapsibleInOutNode();
            node.SetupBuildAndUpdate(nodeModel, null, null);

            var ports = node.Q("inputs").Query(className: "ge-port").ToList();
            Assert.AreEqual(originalInputPortCount, ports.Count);

            ports = node.Q("outputs").Query(className: "ge-port").ToList();
            Assert.AreEqual(originalOutputPortCount, ports.Count);

            const int newInputPortCount = 1;
            const int newOutputPortCount = 3;
            nodeModel.InputCount = newInputPortCount;
            nodeModel.OuputCount = newOutputPortCount;
            nodeModel.DefineNode();
            node.UpdateFromModel();

            ports = node.Q("inputs").Query(className: "ge-port").ToList();
            Assert.AreEqual(newInputPortCount, ports.Count);

            ports = node.Q("outputs").Query(className: "ge-port").ToList();
            Assert.AreEqual(newOutputPortCount, ports.Count);
        }
    }
}
