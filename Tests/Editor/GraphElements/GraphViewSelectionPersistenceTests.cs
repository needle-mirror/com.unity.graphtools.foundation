using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GraphElements
{
    class GraphViewSelectionPersistenceTests : GraphViewTester
    {
        public GraphViewSelectionPersistenceTests() : base(enablePersistence: true) { }

        const string key1 = "node1";
        const string key2 = "node2";
        const string key3 = "node3";

        INodeModel node1Model;
        INodeModel node2Model;
        INodeModel node3Model;

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
            MarkGraphViewStateDirty();
            yield return null;
            GetNodesAndSetViewDataKey(out Node node1, out Node node2, out Node node3);

            Assert.IsNotNull(node1);
            Assert.IsNotNull(node2);
            Assert.IsNotNull(node3);

            // Add two nodes to selection.
            CommandDispatcher.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Add, node1Model, node3Model));

            // Allow 1 frame to let the persistent data get saved
            yield return null;

            // This will re-create the window, flushing all temporary state
            yield return new EnterPlayMode();

            // Allow 1 frame to let the persistence be restored
            yield return null;

            MarkGraphViewStateDirty();
            yield return null;
            GetNodesAndSetViewDataKey(out node1, out node2, out node3);

            Assert.IsNotNull(node1);
            Assert.IsNotNull(node2);
            Assert.IsNotNull(node3);

            Assert.True(node1.IsSelected());
            Assert.False(node2.IsSelected());
            Assert.True(node3.IsSelected());
        }

        [UnityTest, Ignore("FIXME EnterPlayMode")]
        public IEnumerator SelectionIsRestoredWhenEnteringPlaymode_AddNodesBeforePersistence()
        {
            MarkGraphViewStateDirty();
            yield return null;
            GetNodesAndSetViewDataKey(out Node node1, out Node node2, out Node node3);

            CommandDispatcher.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Add, node1Model, node3Model));

            // Allow 1 frame to let the persistent data get saved
            yield return null;

            // This will re-create the window, flushing all temporary state
            yield return new EnterPlayMode();

            node1Model = CreateNode(key1, new Vector2(200, 200));
            node2Model = CreateNode(key2, new Vector2(400, 400));
            node3Model = CreateNode(key3, new Vector2(600, 600));

            MarkGraphViewStateDirty();
            yield return null;
            GetNodesAndSetViewDataKey(out node1, out node2, out node3);

            // Allow 1 frame to let the persistence be restored
            yield return null;

            Assert.True(node1.IsSelected());
            Assert.False(node2.IsSelected());
            Assert.True(node3.IsSelected());
        }

        [UnityTest]
        public IEnumerator CanUndoSelection()
        {
            MarkGraphViewStateDirty();
            yield return null;
            GetNodesAndSetViewDataKey(out Node node1, out Node node2, out Node node3);

            CommandDispatcher.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Add, node1Model, node3Model));

            Undo.PerformUndo();

            Assert.False(node1.IsSelected());
            Assert.False(node2.IsSelected());
            Assert.False(node3.IsSelected());
        }

        [UnityTest]
        public IEnumerator CanRedoSelection()
        {
            MarkGraphViewStateDirty();
            yield return null;
            GetNodesAndSetViewDataKey(out Node node1, out Node node2, out Node node3);

            CommandDispatcher.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Add, node1Model, node3Model));

            Undo.PerformUndo();
            Undo.PerformRedo();

            Assert.True(node1.IsSelected());
            Assert.False(node2.IsSelected());
            Assert.True(node3.IsSelected());
        }

        [UnityTest, Ignore("FIXME EnterPlayMode")]
        public IEnumerator CanRedoSelectionAndEnterPlayMode()
        {
            // Note: this somewhat complex use case ensure that selection for redo
            // and persisted selection are kep in sync
            MarkGraphViewStateDirty();
            yield return null;
            GetNodesAndSetViewDataKey(out Node node1, out Node node2, out Node node3);

            CommandDispatcher.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Add, node1Model, node3Model));

            Undo.PerformUndo();
            Undo.PerformRedo();

            Assert.True(node1.IsSelected());
            Assert.False(node2.IsSelected());
            Assert.True(node3.IsSelected());

            // Allow 1 frame to let the persistence be saved
            yield return null;

            // This will re-create the window, flushing all temporary state
            yield return new EnterPlayMode();

            node1Model = CreateNode(key1, new Vector2(200, 200));
            node2Model = CreateNode(key2, new Vector2(400, 400));
            node3Model = CreateNode(key3, new Vector2(600, 600));

            MarkGraphViewStateDirty();
            yield return null;
            GetNodesAndSetViewDataKey(out node1, out node2, out node3);

            // Allow 1 frame to let the persistence be restored
            yield return null;

            Assert.True(node1.IsSelected());
            Assert.False(node2.IsSelected());
            Assert.True(node3.IsSelected());
        }

        [UnityTest, Ignore("FIXME EnterPlayMode needs backing asset.")]
        public IEnumerator BlackboardSelectionIsRestoredWhenEnteringPlaymode_AddFieldsBeforeAddingBBToGV()
        {
            { // Create initial blackboard.
                var blackboard = new Blackboard();
                blackboard.SetupBuildAndUpdate(null, CommandDispatcher, graphView);

                var inSection = new BlackboardSection(blackboard, "Section 1");
                blackboard.Add(inSection);

                var field = new BlackboardField { viewDataKey = "bfield" };
                var propertyView = new Label("Prop");
                var row = new BlackboardRow();
                row.Add(field);
                row.Add(propertyView);
                inSection.Add(row);

                graphView.AddElement(blackboard);

                CommandDispatcher.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Add, row.Model));

                Assert.True(row.IsSelected());
            }

            // Allow 1 frame to let the persistent data get saved
            yield return null;

            // This will re-create the window, flushing all temporary state
            yield return new EnterPlayMode();

            // Allow 1 frame to let the persistence be restored
            yield return null;

            { // Add field to blackboard first then add blackboard to graphview.
                var blackboard = new Blackboard();
                blackboard.SetupBuildAndUpdate(null, CommandDispatcher, graphView);

                var inSection = new BlackboardSection(blackboard, "Section 1");
                blackboard.Add(inSection);

                var field = new BlackboardField { viewDataKey = "bfield" };
                var propertyView = new Label("Prop");
                var row = new BlackboardRow();
                row.Add(field);
                row.Add(propertyView);
                inSection.Add(row);

                graphView.AddElement(blackboard);

                Assert.True(row.IsSelected());
            }
        }

        [UnityTest, Ignore("FIXME EnterPlayMode needs backing asset.")]
        public IEnumerator BlackboardSelectionIsRestoredWhenEnteringPlaymode_AddFieldsAfterAddingBBToGV()
        {
            { // Create initial blackboard.
                var blackboard = new Blackboard();
                blackboard.SetupBuildAndUpdate(null, CommandDispatcher, graphView);

                var inSection = new BlackboardSection(blackboard, "Section 1");
                blackboard.Add(inSection);

                var field = new BlackboardField { viewDataKey = "bfield" };
                var propertyView = new Label("Prop");
                var row = new BlackboardRow();
                row.Add(field);
                row.Add(propertyView);
                inSection.Add(row);

                graphView.AddElement(blackboard);

                CommandDispatcher.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Add, row.Model));

                Assert.True(row.IsSelected());
            }

            // Allow 1 frame to let the persistent data get saved
            yield return null;

            // This will re-create the window, flushing all temporary state
            yield return new EnterPlayMode();

            // Allow 1 frame to let the persistence be restored
            yield return null;

            { // Add blackboard to graphview first then add field to blackboard.
                var blackboard = new Blackboard();
                blackboard.SetupBuildAndUpdate(null, CommandDispatcher, graphView);

                graphView.AddElement(blackboard);

                var inSection = new BlackboardSection(blackboard, "Section 1");
                blackboard.Add(inSection);

                var field = new BlackboardField { viewDataKey = "bfield" };
                var propertyView = new Label("Prop");
                var row = new BlackboardRow();
                row.Add(field);
                row.Add(propertyView);
                inSection.Add(row);

                Assert.True(row.IsSelected());
            }
        }
    }
}
