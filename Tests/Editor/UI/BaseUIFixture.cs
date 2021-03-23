using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using NUnit.Framework;
using UnityEditor.Experimental.GraphView;
using UnityEditor.VisualScripting.Editor;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model;
using UnityEditor.VisualScripting.Model.Stencils;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.VisualScriptingTests.UI
{
    class VseWindowTest : VseWindow
    {
        protected override IEditorDataModel CreateDataModel()
        {
            return new TestEditorDataModel();
        }

        protected override State CreateInitialState()
        {
            return new TestState(DataModel);
        }
    }

    [PublicAPI]
    [Category("UI")]
    abstract class BaseUIFixture
    {
        const int k_PanAreaWidth = 100;
        static readonly Rect k_WindowRect = new Rect(Vector2.zero, new Vector2(k_PanAreaWidth * 8, k_PanAreaWidth * 6));
        protected VseWindow Window { get; set; }
        protected VseGraphView GraphView { get; private set; }
        protected TestEventHelpers Helpers { get; set; }
        protected Store Store => Window.Store;
        protected VSGraphModel GraphModel => (VSGraphModel)Store.GetState().CurrentGraphModel;

        protected abstract bool CreateGraphOnStartup { get; }
        protected virtual Type CreatedGraphType => typeof(ClassStencil);

        protected class TestContext
        {
            public List<FunctionModel> FunctionModels = new List<FunctionModel>();
            public List<VariableDeclarationModel> VariableDeclModels = new List<VariableDeclarationModel>();
            public List<PortModel> InputPorts = new List<PortModel>();
            public List<PortModel> OutputPorts = new List<PortModel>();

            public void Reset()
            {
                FunctionModels.Clear();
                VariableDeclModels.Clear();
                InputPorts.Clear();
                OutputPorts.Clear();
            }

            public static TestContext Instance { get; } = new TestContext();
        }

        [SetUp]
        public virtual void SetUp()
        {
            var windowWithRect = EditorWindow.GetWindowWithRect<VseWindowTest>(k_WindowRect);
            Window = windowWithRect;
            GraphView = Window.GraphView;
            Helpers = new TestEventHelpers(Window);

            if (CreateGraphOnStartup)
            {
                Store.Dispatch(new CreateGraphAssetAction(CreatedGraphType, "Test", assetPath: string.Empty));
            }
            TestContext.Instance.Reset();
        }

        [TearDown]
        public virtual void TearDown()
        {
            // See case: https://fogbugz.unity3d.com/f/cases/998343/
            // Clearing the capture needs to happen before closing the window
            MouseCaptureController.ReleaseMouse();
            if (Window != null)
            {
                TestContext.Instance.Reset();
                GraphModel.QuickCleanup();
                Window.Close();
            }
        }

        protected IList<GraphElement> GetGraphElements()
        {
            return GraphView.graphElements.ToList();
        }

        protected GraphElement GetGraphElement(int index)
        {
            return GetGraphElements()[index];
        }

        IList<IGraphElementModel> GetGraphElementModels()
        {
            return GetGraphElements()
                .Where(x => x is IHasGraphElementModel)
                .Cast<IHasGraphElementModel>()
                .Select(x => x.GraphElementModel).ToList();
        }

        protected IGraphElementModel GetGraphElementModel(int index)
        {
            return GetGraphElementModels()[index];
        }

        IList<INodeModel> GetNodeModels()
        {
            return GetGraphElements()
                .Where(x => x is IHasGraphElementModel model && model.GraphElementModel is INodeModel)
                .Select(x => ((IHasGraphElementModel)x).GraphElementModel)
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
