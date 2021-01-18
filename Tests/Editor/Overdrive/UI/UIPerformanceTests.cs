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
    class ActionThatMarksNew : BaseAction
    {
        public static void DefaultReducer(State state, ActionThatMarksNew action)
        {
            var placematModel = state.GraphModel.CreatePlacemat(Rect.zero);
            state.MarkNew(placematModel);
        }
    }

    class ActionThatMarksChanged : BaseAction
    {
        public static void DefaultReducer(State state, ActionThatMarksChanged action)
        {
            var placemat = state.GraphModel.PlacematModels.FirstOrDefault();
            Debug.Assert(placemat != null);
            state.MarkChanged(placemat);
        }
    }

    class ActionThatMarksDeleted : BaseAction
    {
        public static void DefaultReducer(State state, ActionThatMarksDeleted action)
        {
            var placemat = state.GraphModel.PlacematModels.FirstOrDefault();
            state.GraphModel.DeletePlacemats(new[] { placemat });
            Debug.Assert(placemat != null);
            state.MarkDeleted(placemat);
        }
    }

    class ActionThatRebuildsAll : BaseAction
    {
        public static void DefaultReducer(State state, ActionThatRebuildsAll action)
        {
            state.RequestUIRebuild();
        }
    }

    class ActionThatDoesNothing : BaseAction
    {
        public static void DefaultReducer(State state, ActionThatDoesNothing action)
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

            Store.State.GraphModel.CreatePlacemat(Rect.zero);

            Store.RegisterReducer<ActionThatMarksNew>(ActionThatMarksNew.DefaultReducer);
            Store.RegisterReducer<ActionThatMarksChanged>(ActionThatMarksChanged.DefaultReducer);
            Store.RegisterReducer<ActionThatMarksDeleted>(ActionThatMarksDeleted.DefaultReducer);
            Store.RegisterReducer<ActionThatRebuildsAll>(ActionThatRebuildsAll.DefaultReducer);
            Store.RegisterReducer<ActionThatDoesNothing>(ActionThatDoesNothing.DefaultReducer);
        }

        static Func<GraphModel, int, Type0FakeNodeModel> MakeNode =>
            (graphModel, i) => graphModel.CreateNode<Type0FakeNodeModel>("Node" + i, Vector2.zero);

        static IEnumerable<object[]> GetSomeActions()
        {
            yield return new object[] { new ActionThatMarksNew(), UIRebuildType.Partial};
            yield return new object[] { new ActionThatMarksChanged(), UIRebuildType.Partial};
            yield return new object[] { new ActionThatMarksDeleted(), UIRebuildType.Partial};
            yield return new object[] { new ActionThatRebuildsAll(), UIRebuildType.Complete};
            yield return new object[] { new ActionThatDoesNothing(), UIRebuildType.None};
        }

        void FakeUpdate()
        {
            Store.BeginViewUpdate();

            var rebuildType = Store.State.GetUpdateType(m_LastStateVersion);
            GraphView.UpdateUI(rebuildType);
            m_LastStateVersion = Store.EndViewUpdate();
        }

        [Test, TestCaseSource(nameof(GetSomeActions))]
        public void TestRebuildType(BaseAction action, UIRebuildType rebuildType)
        {
            // Do the initial update.
            FakeUpdate();

            Store.Dispatch(action);
            FakeUpdate();
            Assert.That(Store.State.LastActionUIRebuildType, Is.EqualTo(rebuildType));

            FakeUpdate();
            Assert.That(Store.State.LastActionUIRebuildType, Is.EqualTo(UIRebuildType.None));
        }

        [UnityTest]
        public IEnumerator TestRebuildIsDoneOnce()
        {
            var model = MakeNode(GraphModel, 0);
            yield return null;
            Assert.That(Store.State.LastActionUIRebuildType, Is.EqualTo(UIRebuildType.Complete));

            yield return null;
            Assert.That(Store.State.LastActionUIRebuildType, Is.EqualTo(UIRebuildType.None));

            Store.Dispatch(new DeleteElementsAction(new[] { model }));
            yield return null;
            Assert.That(Store.State.LastActionUIRebuildType, Is.EqualTo(UIRebuildType.Partial));

            yield return null;
            Assert.That(Store.State.LastActionUIRebuildType, Is.EqualTo(UIRebuildType.None));
        }
    }
}
