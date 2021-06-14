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
    class GtfoWindowTest : GraphViewEditorWindow
    {
        public GtfoWindowTest()
        {
            WithSidePanel = false;
        }

        protected override GraphToolState CreateInitialState()
        {
            var state = base.CreateInitialState();
            var prefs = state.Preferences;
            prefs.SetBoolNoEditorUpdate(BoolPref.ErrorOnRecursiveDispatch, false);
            return state;
        }

        protected override bool CanHandleAssetType(IGraphAssetModel asset)
        {
            return true;
        }
    }

    [PublicAPI]
    [Category("UI")]
    abstract class BaseUIFixture
    {
        protected const string k_GraphPath = "Assets/test.asset";
        const int k_PanAreaWidth = 100;
        static readonly Rect k_WindowRect = new Rect(Vector2.zero, new Vector2(k_PanAreaWidth * 8, k_PanAreaWidth * 6));
        protected GraphViewEditorWindow Window { get; set; }
        protected GraphView GraphView { get; private set; }
        protected TestEventHelpers Helpers { get; set; }
        protected CommandDispatcher CommandDispatcher => Window.CommandDispatcher;
        protected GraphModel GraphModel => (GraphModel)CommandDispatcher.State.WindowState.GraphModel;

        protected abstract bool CreateGraphOnStartup { get; }
        protected virtual Type CreatedGraphType => typeof(ClassStencil);

        [SetUp]
        public virtual void SetUp()
        {
            Window = EditorWindow.GetWindowWithRect<GtfoWindowTest>(k_WindowRect);
            GraphView = Window.GraphView;
            Helpers = new TestEventHelpers(Window);

            if (CreateGraphOnStartup)
            {
                var graphAsset = GraphAssetCreationHelpers<TestGraphAssetModel>.CreateInMemoryGraphAsset(CreatedGraphType, "Test");
                CommandDispatcher.Dispatch(new LoadGraphAssetCommand(graphAsset));
            }
        }

        [TearDown]
        public virtual void TearDown()
        {
            // See case: https://fogbugz.unity3d.com/f/cases/998343/
            // Clearing the capture needs to happen before closing the window
            MouseCaptureController.ReleaseMouse();
            if (Window != null)
            {
                GraphModel?.QuickCleanup();
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

        protected void MarkGraphViewStateDirty()
        {
            using (var updater = CommandDispatcher.State.GraphViewState.UpdateScope)
            {
                updater.ForceCompleteUpdate();
            }
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
                .Where(x => x is IModelUI model && model.Model is INodeModel)
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
        protected IEnumerator TestPrereqCommandPostreq(TestingMode mode, Action checkReqs, Func<int, TestPhase> doUndoableStuff, Action checkPostReqs, int framesToWait = 1)
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
                case TestingMode.Command:
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
