using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.UI
{
    class TestGraphView : GtfoGraphView
    {
        public TestGraphView(GraphViewEditorWindow window, Store store)
            : base(window, store, "TestGraphView") {}

        public override bool CanAcceptDrop(List<ISelectableGraphElement> dragSelection)
        {
            throw new NotImplementedException();
        }

        public override bool DragUpdated(DragUpdatedEvent evt, IEnumerable<ISelectableGraphElement> dragSelection, IDropTarget dropTarget, ISelection dragSource)
        {
            throw new NotImplementedException();
        }

        public override bool DragPerform(DragPerformEvent evt, IEnumerable<ISelectableGraphElement> dragSelection, IDropTarget dropTarget, ISelection dragSource)
        {
            throw new NotImplementedException();
        }

        public override bool DragEnter(DragEnterEvent evt, IEnumerable<ISelectableGraphElement> dragSelection, IDropTarget enteredTarget, ISelection dragSource)
        {
            throw new NotImplementedException();
        }

        public override bool DragLeave(DragLeaveEvent evt, IEnumerable<ISelectableGraphElement> dragSelection, IDropTarget leftTarget, ISelection dragSource)
        {
            throw new NotImplementedException();
        }

        public override bool DragExited()
        {
            throw new NotImplementedException();
        }
    }

    class TestBlankPage : BlankPage
    {
        public TestBlankPage(Store store)
            : base(store) {}
    }

    class GtfoWindowTest : GtfoWindow
    {
        public GtfoWindowTest()
        {
            WithSidePanel = false;
        }

        protected override bool CanHandleAssetType(GraphAssetModel asset)
        {
            return true;
        }

        protected override State CreateInitialState()
        {
            var prefs = TestPreferences.CreatePreferences();
            prefs.SetBoolNoEditorUpdate(BoolPref.ErrorOnRecursiveDispatch, false);
            prefs.SetBoolNoEditorUpdate(BoolPref.ErrorOnMultipleDispatchesPerFrame, false);

            return new State(GUID, prefs);
        }

        protected override BlankPage CreateBlankPage()
        {
            return new TestBlankPage(Store);
        }

        protected override MainToolbar CreateMainToolbar()
        {
            return new MainToolbar(Store, GraphView);
        }

        protected override ErrorToolbar CreateErrorToolbar()
        {
            return new ErrorToolbar(Store, GraphView);
        }

        protected override GtfoGraphView CreateGraphView()
        {
            return new TestGraphView(this, Store);
        }

        protected override Dictionary<Event, ShortcutDelegate> GetShortcutDictionary()
        {
            return new Dictionary<Event, ShortcutDelegate>();
        }
    }

    [PublicAPI]
    [Category("UI")]
    abstract class BaseUIFixture
    {
        protected const string k_GraphPath = "Assets/test.asset";
        const int k_PanAreaWidth = 100;
        static readonly Rect k_WindowRect = new Rect(Vector2.zero, new Vector2(k_PanAreaWidth * 8, k_PanAreaWidth * 6));
        protected GtfoWindow Window { get; set; }
        protected GtfoGraphView GraphView { get; private set; }
        protected TestEventHelpers Helpers { get; set; }
        protected Store Store => Window.Store;
        protected GraphModel GraphModel => (GraphModel)Store.State.GraphModel;

        protected abstract bool CreateGraphOnStartup { get; }
        protected virtual Type CreatedGraphType => typeof(ClassStencil);

        [SetUp]
        public virtual void SetUp()
        {
            var windowWithRect = EditorWindow.GetWindowWithRect<GtfoWindowTest>(k_WindowRect);
            Window = windowWithRect;
            GraphView = Window.GraphView;
            Helpers = new TestEventHelpers(Window);

            if (CreateGraphOnStartup)
            {
                var graphAsset = GraphAssetCreationHelpers<TestGraphAssetModel>.CreateInMemoryGraphAsset(CreatedGraphType, "Test", k_GraphPath);
                Store.Dispatch(new LoadGraphAssetAction(graphAsset));
            }
        }

        [TearDown]
        public void TearDown()
        {
            // See case: https://fogbugz.unity3d.com/f/cases/998343/
            // Clearing the capture needs to happen before closing the window
            MouseCaptureController.ReleaseMouse();
            if (Window != null)
            {
                GraphModel.QuickCleanup();
                Window.Close();
            }
        }

        protected IList<GraphElement> GetGraphElements()
        {
            return GraphView.GraphElements.ToList();
        }

        protected GraphElement GetGraphElement(int index)
        {
            return GetGraphElements()[index];
        }

        IList<IGraphElementModel> GetGraphElementModels()
        {
            return GetGraphElements()
                .Select(x => x.Model).ToList();
        }

        protected IGraphElementModel GetGraphElementModel(int index)
        {
            return GetGraphElementModels()[index];
        }

        IList<INodeModel> GetNodeModels()
        {
            return GetGraphElements()
                .Where(x => x is IGraphElement model && model.Model is INodeModel)
                .Select(x => x.Model)
                .Cast<INodeModel>()
                .ToList();
        }

        protected INodeModel GetNodeModel(int index)
        {
            return GetNodeModels()[index];
        }

        internal enum TestPhase
        {
            WaitForNextFrame,
            Done,
        }
        protected IEnumerator TestPrereqActionPostreq(TestingMode mode, Action checkReqs, Func<int, TestPhase> doUndoableStuff, Action checkPostReqs, int framesToWait = 1)
        {
            yield return null;

            IEnumerator WaitFrames()
            {
                for (int i = 0; i < framesToWait; ++i)
                    yield return null;
            }

            int currentFrame;
            switch (mode)
            {
                case TestingMode.Action:
                    BaseFixture.AssumePreviousTest(checkReqs);

                    currentFrame = 0;
                    while (doUndoableStuff(currentFrame++) == TestPhase.WaitForNextFrame)
                        yield return null;

                    yield return WaitFrames();

                    checkPostReqs();
                    break;

                case TestingMode.UndoRedo:
                    Undo.ClearAll();

                    Undo.IncrementCurrentGroup();
                    BaseFixture.AssumePreviousTest(checkReqs);

                    currentFrame = 0;
                    while (doUndoableStuff(currentFrame++) == TestPhase.WaitForNextFrame)
                        yield return null;

                    Undo.IncrementCurrentGroup();

                    yield return WaitFrames();

                    BaseFixture.AssumePreviousTest(checkPostReqs);

                    yield return WaitFrames();

                    Undo.PerformUndo();

                    yield return WaitFrames();

                    BaseFixture.AssertPreviousTest(checkReqs);

                    Undo.PerformRedo();

                    yield return WaitFrames();

                    BaseFixture.AssertPreviousTest(checkPostReqs);
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
