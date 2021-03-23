using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.UI
{
    class EdgeTests : BaseUIFixture
    {
        protected override bool CreateGraphOnStartup => true;

        [UnityTest]
        public IEnumerator SelectingSourceNodeShowsEdgeOrder()
        {
            var nodeModel1 = GraphModel.CreateNode<Type0FakeNodeModel>("Node0", new Vector2(0, 0));
            var nodeModel2 = GraphModel.CreateNode<Type0FakeNodeModel>("Node1", new Vector2(210, 0));
            var nodeModel3 = GraphModel.CreateNode<Type0FakeNodeModel>("Node2", new Vector2(210, 210));
            var edge1 = GraphModel.CreateEdge(nodeModel2.ExeInput0, nodeModel1.ExeOutput0);
            var edge2 = GraphModel.CreateEdge(nodeModel3.ExeInput0, nodeModel1.ExeOutput0);
            MarkGraphViewStateDirty();
            yield return null;

            Assert.IsTrue(string.IsNullOrEmpty(edge1.EdgeLabel));
            Assert.IsTrue(string.IsNullOrEmpty(edge2.EdgeLabel));

            CommandDispatcher.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Replace, nodeModel1));
            yield return null;

            Assert.IsFalse(string.IsNullOrEmpty(edge1.EdgeLabel));
            Assert.IsFalse(string.IsNullOrEmpty(edge2.EdgeLabel));
        }

        [UnityTest]
        public IEnumerator SelectingDestinationNodeDoesNotShowEdgeOrder()
        {
            var nodeModel1 = GraphModel.CreateNode<Type0FakeNodeModel>("Node0", new Vector2(0, 0));
            var nodeModel2 = GraphModel.CreateNode<Type0FakeNodeModel>("Node1", new Vector2(210, 0));
            var nodeModel3 = GraphModel.CreateNode<Type0FakeNodeModel>("Node2", new Vector2(210, 210));
            var edge1 = GraphModel.CreateEdge(nodeModel2.ExeInput0, nodeModel1.ExeOutput0);
            var edge2 = GraphModel.CreateEdge(nodeModel3.ExeInput0, nodeModel1.ExeOutput0);
            MarkGraphViewStateDirty();
            yield return null;

            Assert.IsTrue(string.IsNullOrEmpty(edge1.EdgeLabel));
            Assert.IsTrue(string.IsNullOrEmpty(edge2.EdgeLabel));

            CommandDispatcher.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Replace, nodeModel2));
            yield return null;

            Assert.IsTrue(string.IsNullOrEmpty(edge1.EdgeLabel));
            Assert.IsTrue(string.IsNullOrEmpty(edge2.EdgeLabel));
        }

        [UnityTest]
        public IEnumerator SelectingEdgeShowsEdgeOrderOnSelfAndSibling()
        {
            var nodeModel1 = GraphModel.CreateNode<Type0FakeNodeModel>("Node0", new Vector2(0, 0));
            var nodeModel2 = GraphModel.CreateNode<Type0FakeNodeModel>("Node1", new Vector2(210, 0));
            var nodeModel3 = GraphModel.CreateNode<Type0FakeNodeModel>("Node2", new Vector2(210, 210));
            var edge1 = GraphModel.CreateEdge(nodeModel2.ExeInput0, nodeModel1.ExeOutput0);
            var edge2 = GraphModel.CreateEdge(nodeModel3.ExeInput0, nodeModel1.ExeOutput0);
            MarkGraphViewStateDirty();
            yield return null;

            Assert.IsTrue(string.IsNullOrEmpty(edge1.EdgeLabel));
            Assert.IsTrue(string.IsNullOrEmpty(edge2.EdgeLabel));

            CommandDispatcher.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Replace, edge1));
            yield return null;

            Assert.IsFalse(string.IsNullOrEmpty(edge1.EdgeLabel));
            Assert.IsFalse(string.IsNullOrEmpty(edge2.EdgeLabel));
        }
    }
}
