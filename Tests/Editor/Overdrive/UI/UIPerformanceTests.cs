using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEngine;
using UnityEngine.TestTools;

// ReSharper disable AccessToStaticMemberViaDerivedType

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.UI
{
    class CommandThatMarksNew : Command
    {
        public static void DefaultCommandHandler(GraphToolState graphToolState, CommandThatMarksNew command)
        {
            var placematModel = graphToolState.GraphModel.CreatePlacemat(Rect.zero);
            graphToolState.MarkNew(placematModel);
        }
    }

    class CommandThatMarksChanged : Command
    {
        public static void DefaultCommandHandler(GraphToolState graphToolState, CommandThatMarksChanged command)
        {
            var placemat = graphToolState.GraphModel.PlacematModels.FirstOrDefault();
            Debug.Assert(placemat != null);
            graphToolState.MarkChanged(placemat);
        }
    }

    class CommandThatMarksDeleted : Command
    {
        public static void DefaultCommandHandler(GraphToolState graphToolState, CommandThatMarksDeleted command)
        {
            var placemat = graphToolState.GraphModel.PlacematModels.FirstOrDefault();
            graphToolState.GraphModel.DeletePlacemats(new[] { placemat });
            Debug.Assert(placemat != null);
            graphToolState.MarkDeleted(placemat);
        }
    }

    class CommandThatRebuildsAll : Command
    {
        public static void DefaultCommandHandler(GraphToolState graphToolState, CommandThatRebuildsAll command)
        {
            graphToolState.RequestUIRebuild();
        }
    }

    class CommandThatDoesNothing : Command
    {
        public static void DefaultCommandHandler(GraphToolState graphToolState, CommandThatDoesNothing command)
        {
        }
    }

    [SuppressMessage("ReSharper", "ConvertToLocalFunction")]
    class UIPerformanceTests : BaseUIFixture
    {
        uint m_LastStateVersion;
        protected override bool CreateGraphOnStartup => true;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            CommandDispatcher.GraphToolState.GraphModel.CreatePlacemat(Rect.zero);

            CommandDispatcher.RegisterCommandHandler<CommandThatMarksNew>(CommandThatMarksNew.DefaultCommandHandler);
            CommandDispatcher.RegisterCommandHandler<CommandThatMarksChanged>(CommandThatMarksChanged.DefaultCommandHandler);
            CommandDispatcher.RegisterCommandHandler<CommandThatMarksDeleted>(CommandThatMarksDeleted.DefaultCommandHandler);
            CommandDispatcher.RegisterCommandHandler<CommandThatRebuildsAll>(CommandThatRebuildsAll.DefaultCommandHandler);
            CommandDispatcher.RegisterCommandHandler<CommandThatDoesNothing>(CommandThatDoesNothing.DefaultCommandHandler);
        }

        static Func<GraphModel, int, Type0FakeNodeModel> MakeNode =>
            (graphModel, i) => graphModel.CreateNode<Type0FakeNodeModel>("Node" + i, Vector2.zero);

        static IEnumerable<object[]> GetSomeCommands()
        {
            yield return new object[] { new CommandThatMarksNew(), UIRebuildType.Partial};
            yield return new object[] { new CommandThatMarksChanged(), UIRebuildType.Partial};
            yield return new object[] { new CommandThatMarksDeleted(), UIRebuildType.Partial};
            yield return new object[] { new CommandThatRebuildsAll(), UIRebuildType.Complete};
            yield return new object[] { new CommandThatDoesNothing(), UIRebuildType.None};
        }

        void FakeUpdate()
        {
            CommandDispatcher.BeginViewUpdate();

            var rebuildType = CommandDispatcher.GraphToolState.GetUpdateType(m_LastStateVersion);
            GraphView.UpdateUI(rebuildType);
            m_LastStateVersion = CommandDispatcher.EndViewUpdate();
        }

        [Test, TestCaseSource(nameof(GetSomeCommands))]
        public void TestRebuildType(Command command, UIRebuildType rebuildType)
        {
            // Do the initial update.
            FakeUpdate();

            CommandDispatcher.Dispatch(command);
            FakeUpdate();
            Assert.That(CommandDispatcher.GraphToolState.LastCommandUIRebuildType, Is.EqualTo(rebuildType));

            FakeUpdate();
            Assert.That(CommandDispatcher.GraphToolState.LastCommandUIRebuildType, Is.EqualTo(UIRebuildType.None));
        }

        [UnityTest]
        public IEnumerator TestRebuildIsDoneOnce()
        {
            var model = MakeNode(GraphModel, 0);
            yield return null;
            Assert.That(CommandDispatcher.GraphToolState.LastCommandUIRebuildType, Is.EqualTo(UIRebuildType.Complete));

            yield return null;
            Assert.That(CommandDispatcher.GraphToolState.LastCommandUIRebuildType, Is.EqualTo(UIRebuildType.None));

            CommandDispatcher.Dispatch(new DeleteElementsCommand(new[] { model }));
            yield return null;
            Assert.That(CommandDispatcher.GraphToolState.LastCommandUIRebuildType, Is.EqualTo(UIRebuildType.Partial));

            yield return null;
            Assert.That(CommandDispatcher.GraphToolState.LastCommandUIRebuildType, Is.EqualTo(UIRebuildType.None));
        }
    }
}
