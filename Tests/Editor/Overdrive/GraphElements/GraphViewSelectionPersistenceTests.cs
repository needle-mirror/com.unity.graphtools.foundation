using System;
using System.Collections;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.GraphElements;
using UnityEditor.GraphToolsFoundation.Overdrive.Tests.GraphElements.Utilities;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GraphElements
{
    public class GraphViewSelectionPersistenceTests : GraphViewTester
    {
        public GraphViewSelectionPersistenceTests() : base(enablePersistence: true) {}

        const string key1 = "node1";
        const string key2 = "node2";
        const string key3 = "node3";

        BasicNodeModel node1Model;
        BasicNodeModel node2Model;
        BasicNodeModel node3Model;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            // When using the EnterPlayMode yield instruction, the SetUp() of the test is ran again
            // In this case, we skip this to be in control of when nodes are created
            if (EditorApplication.isPlaying)
                return;

            node1Model = CreateNode(key1, new Vector2(200, 200));
            node2Model = CreateNode(key2, new Vector2(400, 400));
            node3Model = CreateNode(key3, new Vector2(600, 600));
        }

        [TearDown]
        public override void TearDown()
        {
            base.TearDown();

            Undo.ClearAll();
        }

        void GetNodesAndSetViewDataKey(out Node node1, out Node node2, out Node node3)
        {
            node1 = node1Model.GetUI<Node>(graphView);
            if (node1 != null)
                node1.viewDataKey = key1;

            node2 = node2Model.GetUI<Node>(graphView);
            if (node2 != null)
                node2.viewDataKey = key2;

            node3 = node3Model.GetUI<Node>(graphView);
            if (node3 != null)
                node3.viewDataKey = key3;
        }

        [UnityTest, Ignore("FIXME EnterPlayMode")]
        public IEnumerator SelectionIsRestoredWhenEnteringPlaymode_AddNodesAfterPersistence()
        {
            graphView.RebuildUI(GraphModel, Store);
            yield return null;
            GetNodesAndSetViewDataKey(out Node node1, out Node node2, out Node node3);

            Assert.IsNotNull(node1);
            Assert.IsNotNull(node2);
            Assert.IsNotNull(node3);

            // Add two nodes to selection.
            graphView.AddToSelection(node1);
            graphView.AddToSelection(node3);

            // Allow 1 frame to let the persistent data get saved
            yield return null;

            // This will re-create the window, flushing all temporary state
            yield return new EnterPlayMode();

            // Allow 1 frame to let the persistence be restored
            yield return null;

            graphView.RebuildUI(GraphModel, Store);
            yield return null;
            GetNodesAndSetViewDataKey(out node1, out node2, out node3);

            Assert.IsNotNull(node1);
            Assert.IsNotNull(node2);
            Assert.IsNotNull(node3);

            Assert.True(node1.Selected);
            Assert.False(node2.Selected);
            Assert.True(node3.Selected);
        }

        [UnityTest, Ignore("FIXME EnterPlayMode")]
        public IEnumerator SelectionIsRestoredWhenEnteringPlaymode_AddNodesBeforePersistence()
        {
            graphView.RebuildUI(GraphModel, Store);
            yield return null;
            GetNodesAndSetViewDataKey(out Node node1, out Node node2, out Node node3);

            graphView.AddToSelection(node1);
            graphView.AddToSelection(node3);

            // Allow 1 frame to let the persistent data get saved
            yield return null;

            // This will re-create the window, flushing all temporary state
            yield return new EnterPlayMode();

            node1Model = CreateNode(key1, new Vector2(200, 200));
            node2Model = CreateNode(key2, new Vector2(400, 400));
            node3Model = CreateNode(key3, new Vector2(600, 600));

            graphView.RebuildUI(GraphModel, Store);
            yield return null;
            GetNodesAndSetViewDataKey(out node1, out node2, out node3);

            // Allow 1 frame to let the persistence be restored
            yield return null;

            Assert.True(node1.Selected);
            Assert.False(node2.Selected);
            Assert.True(node3.Selected);
        }

        [UnityTest]
        public IEnumerator CanUndoSelection()
        {
            graphView.RebuildUI(GraphModel, Store);
            yield return null;
            GetNodesAndSetViewDataKey(out Node node1, out Node node2, out Node node3);

            graphView.AddToSelection(node1);
            graphView.AddToSelection(node3);

            Undo.PerformUndo();

            Assert.False(node1.Selected);
            Assert.False(node2.Selected);
            Assert.False(node3.Selected);

            yield return null;
        }

        [UnityTest]
        public IEnumerator CanRedoSelection()
        {
            graphView.RebuildUI(GraphModel, Store);
            yield return null;
            GetNodesAndSetViewDataKey(out Node node1, out Node node2, out Node node3);

            graphView.AddToSelection(node1);
            graphView.AddToSelection(node3);

            Undo.PerformUndo();
            Undo.PerformRedo();

            Assert.True(node1.Selected);
            Assert.False(node2.Selected);
            Assert.True(node3.Selected);

            yield return null;
        }

        [UnityTest, Ignore("FIXME EnterPlayMode")]
        public IEnumerator CanRedoSelectionAndEnterPlayMode()
        {
            // Note: this somewhat complex use case ensure that selection for redo
            // and persisted selection are kep in sync
            graphView.RebuildUI(GraphModel, Store);
            yield return null;
            GetNodesAndSetViewDataKey(out Node node1, out Node node2, out Node node3);

            graphView.AddToSelection(node1);
            graphView.AddToSelection(node3);

            Undo.PerformUndo();
            Undo.PerformRedo();

            Assert.True(node1.Selected);
            Assert.False(node2.Selected);
            Assert.True(node3.Selected);

            // Allow 1 frame to let the persistence be saved
            yield return null;

            // This will re-create the window, flushing all temporary state
            yield return new EnterPlayMode();

            node1Model = CreateNode(key1, new Vector2(200, 200));
            node2Model = CreateNode(key2, new Vector2(400, 400));
            node3Model = CreateNode(key3, new Vector2(600, 600));

            graphView.RebuildUI(GraphModel, Store);
            yield return null;
            GetNodesAndSetViewDataKey(out node1, out node2, out node3);

            // Allow 1 frame to let the persistence be restored
            yield return null;

            Assert.True(node1.Selected);
            Assert.False(node2.Selected);
            Assert.True(node3.Selected);
        }

        [UnityTest, Ignore("FIXME EnterPlayMode needs backing asset.")]
        public IEnumerator BlackboardSelectionIsRestoredWhenEnteringPlaymode_AddFieldsBeforeAddingBBToGV()
        {
            { // Create initial blackboard.
                var blackboard = new Blackboard(Store, graphView);

                var inSection = new BlackboardSection();
                blackboard.Add(inSection);

                var field = new BlackboardField { viewDataKey = "bfield" };
                var propertyView = new Label("Prop");
                var row = new BlackboardRow(field, propertyView);
                inSection.Add(row);

                graphView.AddElement(blackboard);

                graphView.AddToSelection(field);
                Assert.True(field.Selected);
            }

            // Allow 1 frame to let the persistent data get saved
            yield return null;

            // This will re-create the window, flushing all temporary state
            yield return new EnterPlayMode();

            // Allow 1 frame to let the persistence be restored
            yield return null;

            { // Add field to blackboard first then add blackboard to graphview.
                var blackboard = new Blackboard(Store, graphView);

                var inSection = new BlackboardSection();
                blackboard.Add(inSection);

                var field = new BlackboardField { viewDataKey = "bfield" };
                var propertyView = new Label("Prop");
                var row = new BlackboardRow(field, propertyView);
                inSection.Add(row);

                graphView.AddElement(blackboard);

                Assert.True(field.Selected);
            }
        }

        [UnityTest, Ignore("FIXME EnterPlayMode needs backing asset.")]
        public IEnumerator BlackboardSelectionIsRestoredWhenEnteringPlaymode_AddFieldsAfterAddingBBToGV()
        {
            { // Create initial blackboard.
                var blackboard = new Blackboard(Store, graphView);

                var inSection = new BlackboardSection();
                blackboard.Add(inSection);

                var field = new BlackboardField { viewDataKey = "bfield" };
                var propertyView = new Label("Prop");
                var row = new BlackboardRow(field, propertyView);
                inSection.Add(row);

                graphView.AddElement(blackboard);

                graphView.AddToSelection(field);
                Assert.True(field.Selected);
            }

            // Allow 1 frame to let the persistent data get saved
            yield return null;

            // This will re-create the window, flushing all temporary state
            yield return new EnterPlayMode();

            // Allow 1 frame to let the persistence be restored
            yield return null;

            { // Add blackboard to graphview first then add field to blackboard.
                var blackboard = new Blackboard(Store, graphView);
                graphView.AddElement(blackboard);

                var inSection = new BlackboardSection();
                blackboard.Add(inSection);

                var field = new BlackboardField { viewDataKey = "bfield" };
                var propertyView = new Label("Prop");
                var row = new BlackboardRow(field, propertyView);
                inSection.Add(row);

                Assert.True(field.Selected);
            }
        }
    }
}
