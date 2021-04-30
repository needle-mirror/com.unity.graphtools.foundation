using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.Bridge;
using UnityEditor.GraphToolsFoundation.Overdrive.Tests.TestModels;
using UnityEditor.VersionControl;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GraphElements
{
    class GraphContextTests : GraphViewTester
    {
        IContextNodeModel m_Node;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            m_Node = CreateContext<ContextNodeModel>("context", Vector2.zero);
        }

        [Test]
        public void CreateContextFromModelGivesContext()
        {
            Assert.IsFalse(m_Node.IsCollapsible());

            graphView.RebuildUI(GraphModel, CommandDispatcher);
            List<Node> nodeList = graphView.Nodes.ToList();

            // Nothing is connected. The collapse button should be enabled.
            Assert.AreEqual(1, nodeList.Count);

            Assert.IsTrue(nodeList.First() is ContextNode);
        }
    }
}

