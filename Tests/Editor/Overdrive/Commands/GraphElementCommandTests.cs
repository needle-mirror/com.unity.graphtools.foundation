using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.Commands
{
    public class GraphElementCommandTests : BaseFixture
    {
        protected override bool CreateGraphOnStartup => true;

        [Test]
        public void ReplacesCurrentSelectionWorksAndDirtiesElements([Values] TestingMode mode)
        {
            var node0 = GraphModel.CreateNode<Type0FakeNodeModel>("Node0", new Vector2(-200, 0));
            var node1 = GraphModel.CreateNode<Type0FakeNodeModel>("Node1", new Vector2(200, 0));

            m_CommandDispatcher.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Replace, node0, node1));
            m_CommandDispatcher.GraphToolState.PurgeAllChangesets();
            var changeset = m_CommandDispatcher.GraphToolState.SelectionState.GetAggregatedChangeset(0);
            Assert.AreEqual(0, changeset.ChangedModels.Count());
            var currentVersion = m_CommandDispatcher.GraphToolState.SelectionState.GetStateComponentVersion();

            TestPrereqCommandPostreq(mode,
                () =>
                {
                    Assert.IsTrue(m_CommandDispatcher.GraphToolState.SelectionState.IsSelected(node0));
                    Assert.IsTrue(m_CommandDispatcher.GraphToolState.SelectionState.IsSelected(node1));

                    return new SelectElementsCommand(SelectElementsCommand.SelectionMode.Replace, node0);
                },
                () =>
                {
                    Assert.IsTrue(m_CommandDispatcher.GraphToolState.SelectionState.IsSelected(node0));
                    Assert.IsFalse(m_CommandDispatcher.GraphToolState.SelectionState.IsSelected(node1));

                    if (m_CommandDispatcher.GraphToolState.LastDispatchedCommandName == nameof(SelectElementsCommand))
                    {
                        changeset = m_CommandDispatcher.GraphToolState.SelectionState.GetAggregatedChangeset(currentVersion.Version);

                        Assert.AreEqual(UpdateType.Partial,
                            m_CommandDispatcher.GraphToolState.SelectionState.GetUpdateType(currentVersion));

                        Assert.IsTrue(changeset.ChangedModels.Contains(node0));
                        Assert.IsTrue(changeset.ChangedModels.Contains(node1));
                    }
                    else if (m_CommandDispatcher.GraphToolState.LastDispatchedCommandName == nameof(UndoRedoCommand))
                    {
                        Assert.AreEqual(UpdateType.Complete,
                            m_CommandDispatcher.GraphToolState.SelectionState.GetUpdateType(currentVersion));
                    }
                    else
                    {
                        Assert.Fail("Unexpected command name");
                    }
                });
        }

        [Test]
        public void AddToSelectionWorksAndDirtiesElements([Values] TestingMode mode)
        {
            var node0 = GraphModel.CreateNode<Type0FakeNodeModel>("Node0", new Vector2(-200, 0));
            var node1 = GraphModel.CreateNode<Type0FakeNodeModel>("Node1", new Vector2(200, 0));

            m_CommandDispatcher.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Replace, node0));
            m_CommandDispatcher.GraphToolState.PurgeAllChangesets();
            var changeset = m_CommandDispatcher.GraphToolState.SelectionState.GetAggregatedChangeset(0);
            Assert.AreEqual(0, changeset.ChangedModels.Count);
            var currentVersion = m_CommandDispatcher.GraphToolState.SelectionState.GetStateComponentVersion();

            TestPrereqCommandPostreq(mode,
                () =>
                {
                    Assert.IsTrue(m_CommandDispatcher.GraphToolState.SelectionState.IsSelected(node0));
                    Assert.IsFalse(m_CommandDispatcher.GraphToolState.SelectionState.IsSelected(node1));
                    return new SelectElementsCommand(SelectElementsCommand.SelectionMode.Add, node1);
                },
                () =>
                {
                    Assert.IsTrue(m_CommandDispatcher.GraphToolState.SelectionState.IsSelected(node0));
                    Assert.IsTrue(m_CommandDispatcher.GraphToolState.SelectionState.IsSelected(node1));

                    if (m_CommandDispatcher.GraphToolState.LastDispatchedCommandName == nameof(SelectElementsCommand))
                    {
                        changeset = m_CommandDispatcher.GraphToolState.SelectionState.GetAggregatedChangeset(currentVersion.Version);

                        Assert.AreEqual(UpdateType.Partial,
                            m_CommandDispatcher.GraphToolState.SelectionState.GetUpdateType(currentVersion));

                        Assert.IsFalse(changeset.ChangedModels.Contains(node0));
                        Assert.IsTrue(changeset.ChangedModels.Contains(node1));
                    }
                    else if (m_CommandDispatcher.GraphToolState.LastDispatchedCommandName == nameof(UndoRedoCommand))
                    {
                        Assert.AreEqual(UpdateType.Complete,
                            m_CommandDispatcher.GraphToolState.SelectionState.GetUpdateType(currentVersion));
                    }
                    else
                    {
                        Assert.Fail("Unexpected command name");
                    }
                });
        }

        [Test]
        public void RemoveElementFromSelectionWorksAndDirtiesElements([Values] TestingMode mode)
        {
            var node0 = GraphModel.CreateNode<Type0FakeNodeModel>("Node0", new Vector2(-200, 0));
            var node1 = GraphModel.CreateNode<Type0FakeNodeModel>("Node1", new Vector2(200, 0));

            m_CommandDispatcher.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Replace, node0, node1));
            m_CommandDispatcher.GraphToolState.PurgeAllChangesets();
            var changeset = m_CommandDispatcher.GraphToolState.SelectionState.GetAggregatedChangeset(0);
            Assert.AreEqual(0, changeset.ChangedModels.Count());
            var currentVersion = m_CommandDispatcher.GraphToolState.SelectionState.GetStateComponentVersion();

            TestPrereqCommandPostreq(mode,
                () =>
                {
                    Assert.IsTrue(m_CommandDispatcher.GraphToolState.SelectionState.IsSelected(node0));
                    Assert.IsTrue(m_CommandDispatcher.GraphToolState.SelectionState.IsSelected(node1));

                    return new SelectElementsCommand(SelectElementsCommand.SelectionMode.Remove, node1);
                },
                () =>
                {
                    Assert.IsTrue(m_CommandDispatcher.GraphToolState.SelectionState.IsSelected(node0));
                    Assert.IsFalse(m_CommandDispatcher.GraphToolState.SelectionState.IsSelected(node1));

                    if (m_CommandDispatcher.GraphToolState.LastDispatchedCommandName == nameof(SelectElementsCommand))
                    {
                        changeset = m_CommandDispatcher.GraphToolState.SelectionState.GetAggregatedChangeset(currentVersion.Version);

                        Assert.AreEqual(UpdateType.Partial,
                            m_CommandDispatcher.GraphToolState.SelectionState.GetUpdateType(currentVersion));

                        Assert.IsFalse(changeset.ChangedModels.Contains(node0));
                        Assert.IsTrue(changeset.ChangedModels.Contains(node1));
                    }
                    else if (m_CommandDispatcher.GraphToolState.LastDispatchedCommandName == nameof(UndoRedoCommand))
                    {
                        Assert.AreEqual(UpdateType.Complete,
                            m_CommandDispatcher.GraphToolState.SelectionState.GetUpdateType(currentVersion));
                    }
                    else
                    {
                        Assert.Fail("Unexpected command name");
                    }
                });
        }

        [Test]
        public void ToggleSelectionWorksAndDirtiesElements([Values] TestingMode mode)
        {
            var node0 = GraphModel.CreateNode<Type0FakeNodeModel>("Node0", new Vector2(-200, 0));
            var node1 = GraphModel.CreateNode<Type0FakeNodeModel>("Node1", new Vector2(200, 0));

            m_CommandDispatcher.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Replace, node0));
            m_CommandDispatcher.GraphToolState.PurgeAllChangesets();
            var changeset = m_CommandDispatcher.GraphToolState.SelectionState.GetAggregatedChangeset(0);
            Assert.AreEqual(0, changeset.ChangedModels.Count());
            var currentVersion = m_CommandDispatcher.GraphToolState.SelectionState.GetStateComponentVersion();

            TestPrereqCommandPostreq(mode,
                () =>
                {
                    Assert.IsTrue(m_CommandDispatcher.GraphToolState.SelectionState.IsSelected(node0));
                    Assert.IsFalse(m_CommandDispatcher.GraphToolState.SelectionState.IsSelected(node1));

                    return new SelectElementsCommand(SelectElementsCommand.SelectionMode.Toggle, node0, node1);
                },
                () =>
                {
                    Assert.IsFalse(m_CommandDispatcher.GraphToolState.SelectionState.IsSelected(node0));
                    Assert.IsTrue(m_CommandDispatcher.GraphToolState.SelectionState.IsSelected(node1));

                    if (m_CommandDispatcher.GraphToolState.LastDispatchedCommandName == nameof(SelectElementsCommand))
                    {
                        changeset = m_CommandDispatcher.GraphToolState.SelectionState.GetAggregatedChangeset(currentVersion.Version);

                        Assert.AreEqual(UpdateType.Partial,
                            m_CommandDispatcher.GraphToolState.SelectionState.GetUpdateType(currentVersion));

                        Assert.IsTrue(changeset.ChangedModels.Contains(node0));
                        Assert.IsTrue(changeset.ChangedModels.Contains(node1));
                    }
                    else if (m_CommandDispatcher.GraphToolState.LastDispatchedCommandName == nameof(UndoRedoCommand))
                    {
                        Assert.AreEqual(UpdateType.Complete,
                            m_CommandDispatcher.GraphToolState.SelectionState.GetUpdateType(currentVersion));
                    }
                    else
                    {
                        Assert.Fail("Unexpected command name");
                    }
                });
        }

        [Test]
        public void ClearSelectionWorksAndDirtiesElements([Values] TestingMode mode)
        {
            var node0 = GraphModel.CreateNode<Type0FakeNodeModel>("Node0", new Vector2(-200, 0));
            var node1 = GraphModel.CreateNode<Type0FakeNodeModel>("Node1", new Vector2(200, 0));

            m_CommandDispatcher.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Replace, node0));
            m_CommandDispatcher.GraphToolState.PurgeAllChangesets();
            var changeset = m_CommandDispatcher.GraphToolState.SelectionState.GetAggregatedChangeset(0);
            Assert.AreEqual(0, changeset.ChangedModels.Count());
            var currentVersion = m_CommandDispatcher.GraphToolState.SelectionState.GetStateComponentVersion();

            TestPrereqCommandPostreq(mode,
                () =>
                {
                    Assert.IsTrue(m_CommandDispatcher.GraphToolState.SelectionState.IsSelected(node0));
                    Assert.IsFalse(m_CommandDispatcher.GraphToolState.SelectionState.IsSelected(node1));

                    return new ClearSelectionCommand();
                },
                () =>
                {
                    Assert.IsFalse(m_CommandDispatcher.GraphToolState.SelectionState.IsSelected(node0));
                    Assert.IsFalse(m_CommandDispatcher.GraphToolState.SelectionState.IsSelected(node1));

                    if (m_CommandDispatcher.GraphToolState.LastDispatchedCommandName == nameof(ClearSelectionCommand))
                    {
                        changeset = m_CommandDispatcher.GraphToolState.SelectionState.GetAggregatedChangeset(currentVersion.Version);

                        Assert.AreEqual(UpdateType.Partial,
                            m_CommandDispatcher.GraphToolState.SelectionState.GetUpdateType(currentVersion));

                        Assert.IsTrue(changeset.ChangedModels.Contains(node0));
                        Assert.IsFalse(changeset.ChangedModels.Contains(node1));
                    }
                    else if (m_CommandDispatcher.GraphToolState.LastDispatchedCommandName == nameof(UndoRedoCommand))
                    {
                        Assert.AreEqual(UpdateType.Complete,
                            m_CommandDispatcher.GraphToolState.SelectionState.GetUpdateType(currentVersion));
                    }
                    else
                    {
                        Assert.Fail("Unexpected command name");
                    }
                });
        }
    }
}
