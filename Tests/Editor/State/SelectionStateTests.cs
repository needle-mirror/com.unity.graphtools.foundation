using System;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.Tests.TestModels;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.CommandSystem
{
    public class SelectionStateTests
    {
        [Test]
        public void AddingToSelectionWorks()
        {
            var node1 = new NodeModel();
            var node2 = new NodeModel();

            var viewGuid1 = SerializableGUID.Generate();
            var state = new GraphToolState(viewGuid1, null);
            using (var selectionUpdater = state.SelectionState.UpdateScope)
            {
                selectionUpdater.ClearSelection(state.WindowState.GraphModel);
            }

            Assert.IsFalse(state.SelectionState.IsSelected(node1));
            Assert.IsFalse(state.SelectionState.IsSelected(node2));

            using (var selectionUpdater = state.SelectionState.UpdateScope)
            {
                selectionUpdater.SelectElements(new[] { node1 }, true);
            }

            Assert.IsTrue(state.SelectionState.IsSelected(node1));
            Assert.IsFalse(state.SelectionState.IsSelected(node2));
        }

        [Test]
        public void RemovingFromSelectionWorks()
        {
            var node1 = new NodeModel();
            var node2 = new NodeModel();

            var viewGuid1 = SerializableGUID.Generate();
            var state = new GraphToolState(viewGuid1, null);

            using (var selectionUpdater = state.SelectionState.UpdateScope)
            {
                selectionUpdater.ClearSelection(state.WindowState.GraphModel);
                selectionUpdater.SelectElements(new[] { node1, node2 }, true);
            }

            Assert.IsTrue(state.SelectionState.IsSelected(node1));
            Assert.IsTrue(state.SelectionState.IsSelected(node2));

            using (var selectionUpdater = state.SelectionState.UpdateScope)
            {
                selectionUpdater.SelectElements(new[] { node1 }, false);
            }

            Assert.IsFalse(state.SelectionState.IsSelected(node1));
            Assert.IsTrue(state.SelectionState.IsSelected(node2));
        }

        [Test]
        public void ClearSelectionWorks()
        {
            var node1 = new NodeModel();
            var node2 = new NodeModel();

            var viewGuid1 = SerializableGUID.Generate();
            var state = new GraphToolState(viewGuid1, null);

            using (var selectionUpdater = state.SelectionState.UpdateScope)
            {
                selectionUpdater.ClearSelection(state.WindowState.GraphModel);
                selectionUpdater.SelectElements(new[] { node1, node2 }, true);
            }

            Assert.IsTrue(state.SelectionState.IsSelected(node1));
            Assert.IsTrue(state.SelectionState.IsSelected(node2));

            using (var selectionUpdater = state.SelectionState.UpdateScope)
            {
                selectionUpdater.ClearSelection(state.WindowState.GraphModel);
            }

            Assert.IsFalse(state.SelectionState.IsSelected(node1));
            Assert.IsFalse(state.SelectionState.IsSelected(node2));
        }
    }
}
