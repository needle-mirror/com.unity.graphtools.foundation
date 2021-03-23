using System;
using System.Linq;
using NUnit.Framework;
using Object = UnityEngine.Object;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.CommandSystem
{
    public class GraphViewStateTests
    {
        IGraphAssetModel m_Asset1;

        [SetUp]
        public void SetUp()
        {
            m_Asset1 = GraphAssetCreationHelpers<TestGraphAssetModel>.CreateInMemoryGraphAsset(typeof(ClassStencil), "Test1", "Assets/Tests/test1.asset");
        }

        [Test]
        public void EmptyChangeSetsDoNotDirtyAsset()
        {
            var viewGuid1 = GUID.Generate();
            var state = new GraphToolState(viewGuid1, null);
            state.LoadGraphAsset(m_Asset1, null);
            var initialDirtyCount = EditorUtility.GetDirtyCount(m_Asset1 as Object);

            using (var graphUpdater = state.GraphViewState.Updater)
            {
                graphUpdater.U.MarkNew(Enumerable.Empty<IGraphElementModel>());
                graphUpdater.U.MarkChanged(Enumerable.Empty<IGraphElementModel>());
                graphUpdater.U.MarkDeleted(Enumerable.Empty<IGraphElementModel>());
            }

            Assert.AreEqual(initialDirtyCount, EditorUtility.GetDirtyCount(m_Asset1 as Object));
        }

        [Test]
        public void MarkNewRemovesModelFromTheChangedList()
        {
            var viewGuid1 = GUID.Generate();
            var state = new GraphToolState(viewGuid1, null);
            state.LoadGraphAsset(m_Asset1, null);
            var dummyModel = new TestNodeModel();
            using (var graphUpdater = state.GraphViewState.Updater)
            {
                graphUpdater.U.MarkChanged(dummyModel);
            }

            var changes = state.GraphViewState.GetAggregatedChangeset(0);
            Assert.IsTrue(changes.ChangedModels.Contains(dummyModel));

            using (var graphUpdater = state.GraphViewState.Updater)
            {
                graphUpdater.U.MarkNew(dummyModel);
            }

            changes = state.GraphViewState.GetAggregatedChangeset(0);
            Assert.IsFalse(changes.ChangedModels.Contains(dummyModel));
            Assert.IsTrue(changes.NewModels.Contains(dummyModel));
        }

        [Test]
        public void MarkNewHasNoEffectIfModelIsDeleted()
        {
            var viewGuid1 = GUID.Generate();
            var state = new GraphToolState(viewGuid1, null);
            state.LoadGraphAsset(m_Asset1, null);
            var dummyModel = new TestNodeModel();
            using (var graphUpdater = state.GraphViewState.Updater)
            {
                graphUpdater.U.MarkDeleted(dummyModel);
            }

            var changes = state.GraphViewState.GetAggregatedChangeset(0);
            Assert.IsTrue(changes.DeletedModels.Contains(dummyModel));

            using (var graphUpdater = state.GraphViewState.Updater)
            {
                graphUpdater.U.MarkNew(dummyModel);
            }

            changes = state.GraphViewState.GetAggregatedChangeset(0);
            Assert.IsFalse(changes.NewModels.Contains(dummyModel));
        }

        [Test]
        public void MarkChangedHasNoEffectIfModelIsNew()
        {
            var viewGuid1 = GUID.Generate();
            var state = new GraphToolState(viewGuid1, null);
            state.LoadGraphAsset(m_Asset1, null);
            var dummyModel = new TestNodeModel();
            using (var graphUpdater = state.GraphViewState.Updater)
            {
                graphUpdater.U.MarkNew(dummyModel);
            }

            var changes = state.GraphViewState.GetAggregatedChangeset(0);
            Assert.IsTrue(changes.NewModels.Contains(dummyModel));

            using (var graphUpdater = state.GraphViewState.Updater)
            {
                graphUpdater.U.MarkChanged(dummyModel);
            }

            changes = state.GraphViewState.GetAggregatedChangeset(0);
            Assert.IsFalse(changes.ChangedModels.Contains(dummyModel));
        }

        [Test]
        public void MarkChangedHasNoEffectIfModelIsDeleted()
        {
            var viewGuid1 = GUID.Generate();
            var state = new GraphToolState(viewGuid1, null);
            state.LoadGraphAsset(m_Asset1, null);
            var dummyModel = new TestNodeModel();
            using (var graphUpdater = state.GraphViewState.Updater)
            {
                graphUpdater.U.MarkDeleted(dummyModel);
            }

            var changes = state.GraphViewState.GetAggregatedChangeset(0);
            Assert.IsTrue(changes.DeletedModels.Contains(dummyModel));

            using (var graphUpdater = state.GraphViewState.Updater)
            {
                graphUpdater.U.MarkChanged(dummyModel);
            }

            changes = state.GraphViewState.GetAggregatedChangeset(0);
            Assert.IsFalse(changes.ChangedModels.Contains(dummyModel));
        }

        [Test]
        public void MarkDeletedRemovesModelFromTheNewList()
        {
            var viewGuid1 = GUID.Generate();
            var state = new GraphToolState(viewGuid1, null);
            state.LoadGraphAsset(m_Asset1, null);
            var dummyModel = new TestNodeModel();
            using (var graphUpdater = state.GraphViewState.Updater)
            {
                graphUpdater.U.MarkNew(dummyModel);
            }

            var changes = state.GraphViewState.GetAggregatedChangeset(0);
            Assert.IsTrue(changes.NewModels.Contains(dummyModel));

            using (var graphUpdater = state.GraphViewState.Updater)
            {
                graphUpdater.U.MarkDeleted(dummyModel);
            }

            changes = state.GraphViewState.GetAggregatedChangeset(0);
            Assert.IsFalse(changes.NewModels.Contains(dummyModel));
            Assert.IsTrue(changes.DeletedModels.Contains(dummyModel));
        }

        [Test]
        public void MarkDeletedRemovesModelFromTheChangedList()
        {
            var viewGuid1 = GUID.Generate();
            var state = new GraphToolState(viewGuid1, null);
            state.LoadGraphAsset(m_Asset1, null);
            var dummyModel = new TestNodeModel();
            using (var graphUpdater = state.GraphViewState.Updater)
            {
                graphUpdater.U.MarkChanged(dummyModel);
            }

            var changes = state.GraphViewState.GetAggregatedChangeset(0);
            Assert.IsTrue(changes.ChangedModels.Contains(dummyModel));

            using (var graphUpdater = state.GraphViewState.Updater)
            {
                graphUpdater.U.MarkDeleted(dummyModel);
            }

            changes = state.GraphViewState.GetAggregatedChangeset(0);
            Assert.IsFalse(changes.ChangedModels.Contains(dummyModel));
            Assert.IsTrue(changes.DeletedModels.Contains(dummyModel));
        }
    }
}