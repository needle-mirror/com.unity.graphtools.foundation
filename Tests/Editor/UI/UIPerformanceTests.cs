using System;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using NUnit.Framework;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;
using UnityEngine;
using UnityEngine.TestTools;

// ReSharper disable AccessToStaticMemberViaDerivedType

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.UI
{
    class CommandThatMarksNew : UndoableCommand
    {
        public static void DefaultCommandHandler(GraphToolState graphToolState, CommandThatMarksNew command)
        {
            using (var graphUpdater = graphToolState.GraphViewState.UpdateScope)
            {
                var placematModel = graphToolState.GraphViewState.GraphModel.CreatePlacemat(Rect.zero);
                graphUpdater.MarkNew(placematModel);
            }
        }
    }

    class CommandThatMarksChanged : UndoableCommand
    {
        public static void DefaultCommandHandler(GraphToolState graphToolState, CommandThatMarksChanged command)
        {
            using (var graphUpdater = graphToolState.GraphViewState.UpdateScope)
            {
                var placemat = graphToolState.GraphViewState.GraphModel.PlacematModels.FirstOrDefault();
                Debug.Assert(placemat != null);
                graphUpdater.MarkChanged(placemat);
            }
        }
    }

    class CommandThatMarksDeleted : UndoableCommand
    {
        public static void DefaultCommandHandler(GraphToolState graphToolState, CommandThatMarksDeleted command)
        {
            using (var graphUpdater = graphToolState.GraphViewState.UpdateScope)
            {
                var placemat = graphToolState.GraphViewState.GraphModel.PlacematModels.FirstOrDefault();
                graphToolState.GraphViewState.GraphModel.DeletePlacemats(new[] { placemat });
                Debug.Assert(placemat != null);
                graphUpdater.MarkDeleted(placemat);
            }
        }
    }

    class CommandThatRebuildsAll : UndoableCommand
    {
        public static void DefaultCommandHandler(GraphToolState graphToolState, CommandThatRebuildsAll command)
        {
            using (var updater = graphToolState.GraphViewState.UpdateScope)
            {
                updater.ForceCompleteUpdate();
            }
        }
    }

    class CommandThatDoesNothing : UndoableCommand
    {
        public static void DefaultCommandHandler(GraphToolState graphToolState, CommandThatDoesNothing command)
        {
        }
    }

    class GraphViewStateObserver : StateObserver<GraphToolState>
    {
        public UpdateType UpdateType { get; set; }

        /// <inheritdoc />
        public GraphViewStateObserver()
            : base(nameof(GraphToolState.GraphViewState)) { }

        /// <inheritdoc />
        protected override void Observe(GraphToolState state)
        {
            using (var observation = this.ObserveState(state.GraphViewState))
                UpdateType = observation.UpdateType;
        }
    }

    [SuppressMessage("ReSharper", "ConvertToLocalFunction")]
    class UIPerformanceTests : BaseUIFixture
    {
        protected override bool CreateGraphOnStartup => true;

        GraphViewStateObserver m_GraphViewStateObserver;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            CommandDispatcher.State.GraphViewState.GraphModel.CreatePlacemat(Rect.zero);

            CommandDispatcher.RegisterCommandHandler<CommandThatMarksNew>(CommandThatMarksNew.DefaultCommandHandler);
            CommandDispatcher.RegisterCommandHandler<CommandThatMarksChanged>(CommandThatMarksChanged.DefaultCommandHandler);
            CommandDispatcher.RegisterCommandHandler<CommandThatMarksDeleted>(CommandThatMarksDeleted.DefaultCommandHandler);
            CommandDispatcher.RegisterCommandHandler<CommandThatRebuildsAll>(CommandThatRebuildsAll.DefaultCommandHandler);
            CommandDispatcher.RegisterCommandHandler<CommandThatDoesNothing>(CommandThatDoesNothing.DefaultCommandHandler);

            m_GraphViewStateObserver = new GraphViewStateObserver();
            CommandDispatcher.RegisterObserver(m_GraphViewStateObserver);
        }

        [TearDown]
        public override void TearDown()
        {
            CommandDispatcher.UnregisterObserver(m_GraphViewStateObserver);
            base.TearDown();
        }

        static IEnumerable GetSomeCommands()
        {
            yield return new TestCaseData(new CommandThatMarksNew(), UpdateType.Partial).Returns(null);
            yield return new TestCaseData(new CommandThatMarksChanged(), UpdateType.Partial).Returns(null);
            yield return new TestCaseData(new CommandThatMarksDeleted(), UpdateType.Partial).Returns(null);
            yield return new TestCaseData(new CommandThatRebuildsAll(), UpdateType.Complete).Returns(null);
            yield return new TestCaseData(new CommandThatDoesNothing(), UpdateType.None).Returns(null);
        }

        [UnityTest, TestCaseSource(nameof(GetSomeCommands))]
        public IEnumerator TestRebuildType(UndoableCommand command, UpdateType rebuildType)
        {
            // Do the initial update.
            yield return null;

            m_GraphViewStateObserver.UpdateType = UpdateType.None;
            CommandDispatcher.Dispatch(command);
            yield return null;
            Assert.That(m_GraphViewStateObserver.UpdateType, Is.EqualTo(rebuildType));

            m_GraphViewStateObserver.UpdateType = UpdateType.None;
            yield return null;
            Assert.That(m_GraphViewStateObserver.UpdateType, Is.EqualTo(UpdateType.None));
        }

        [UnityTest]
        public IEnumerator TestRebuildIsDoneOnce()
        {
            m_GraphViewStateObserver.UpdateType = UpdateType.None;
            Type0FakeNodeModel model;
            using (var updater = CommandDispatcher.State.GraphViewState.UpdateScope)
            {
                model = GraphModel.CreateNode<Type0FakeNodeModel>("Node 0", Vector2.zero);
                updater.MarkNew(model);
            }
            yield return null;
            Assert.That(m_GraphViewStateObserver.UpdateType, Is.EqualTo(UpdateType.Complete));
            m_GraphViewStateObserver.UpdateType = UpdateType.None;

            yield return null;
            Assert.That(m_GraphViewStateObserver.UpdateType, Is.EqualTo(UpdateType.None));
            m_GraphViewStateObserver.UpdateType = UpdateType.None;

            CommandDispatcher.Dispatch(new DeleteElementsCommand(model));
            yield return null;
            Assert.That(m_GraphViewStateObserver.UpdateType, Is.EqualTo(UpdateType.Partial));
            m_GraphViewStateObserver.UpdateType = UpdateType.None;

            yield return null;
            Assert.That(m_GraphViewStateObserver.UpdateType, Is.EqualTo(UpdateType.None));
        }
    }
}
