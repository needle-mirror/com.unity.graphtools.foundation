using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEngine;
using UnityEngine.Profiling;

// ReSharper disable AccessToStaticMemberViaDerivedType

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests
{
    public enum TestingMode { Action, UndoRedo }

    class TestPreferences : Preferences
    {
        public static TestPreferences CreatePreferences()
        {
            var preferences = new TestPreferences();
            preferences.Initialize<BoolPref, IntPref>();
            return preferences;
        }

        protected override string GetEditorPreferencesPrefix()
        {
            return "GraphToolsFoundationTests.";
        }
    }

    class TestState : State
    {
        public TestState(GUID graphViewEditorWindowGUID, Preferences preferences)
            : base(graphViewEditorWindowGUID, preferences) {}

        public override void PostDispatchAction(BaseAction action)
        {
            base.PostDispatchAction(action);

            if (GraphModel != null)
                Assert.IsTrue(GraphModel.CheckIntegrity(Verbosity.Errors));
        }
    }

    [SetUpFixture]
    class SetUpFixture
    {
        [OneTimeSetUp]
        public void RunBeforeAnyTest()
        {
            AssetWatcher.disabled = true;
        }

        [OneTimeTearDown]
        public void RunAfterAllTests()
        {
            AssetWatcher.disabled = false;
        }
    }

    [PublicAPI]
    public abstract class BaseFixture
    {
        protected Store m_Store;
        protected const string k_GraphPath = "Assets/test.asset";

        protected GraphModel GraphModel => (GraphModel)m_Store.State.GraphModel;
        protected Stencil Stencil => GraphModel.Stencil;

        protected abstract bool CreateGraphOnStartup { get; }
        protected virtual Type CreatedGraphType => typeof(ClassStencil);

        internal static void AssumePreviousTest(Action delegateToRun)
        {
            try
            {
                delegateToRun();
            }
            catch (AssertionException e)
            {
                var inconclusiveException = new InconclusiveException(e.Message + "Delegate stack trace:\n" + e.StackTrace, e);
                throw inconclusiveException;
            }
        }

        internal static void AssertPreviousTest(Action delegateToRun)
        {
            try
            {
                delegateToRun();
            }
            catch (InconclusiveException e)
            {
                var inconclusiveException = new AssertionException(e.Message + "Delegate stack trace:\n" + e.StackTrace, e);
                throw inconclusiveException;
            }
        }

        protected void TestPrereqActionPostreq<T>(TestingMode mode, Action checkReqs, Func<T> provideAction, Action checkPostReqs) where T : BaseAction
        {
            T action;
            switch (mode)
            {
                case TestingMode.Action:
                    checkReqs();
                    action = provideAction();
                    m_Store.Dispatch(action);

                    checkPostReqs();
                    break;
                case TestingMode.UndoRedo:
                    Undo.IncrementCurrentGroup();

                    AssumePreviousTest(() =>
                    {
                        checkReqs();
                        action = provideAction();
                        m_Store.Dispatch(action);
                        checkPostReqs();
                    });

                    Undo.IncrementCurrentGroup();

                    Undo.PerformUndo();

                    CheckUndo(checkReqs, provideAction);

                    Undo.PerformRedo();
                    CheckRedo(checkPostReqs);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        static void CheckRedo(Action checkPostReqs)
        {
            AssertPreviousTest(checkPostReqs);
        }

        static void CheckUndo<T>(Action checkReqs, Func<T> provideAction) where T : BaseAction
        {
            AssertPreviousTest(checkReqs);
            AssertPreviousTest(() => provideAction());
        }

        protected void TestPrereqActionPostreq<T>(TestingMode mode, Func<T> checkReqsAndProvideAction, Action checkPostReqs) where T : BaseAction
        {
            TestPrereqActionPostreq(mode, () => {}, checkReqsAndProvideAction, checkPostReqs);
        }

        [SetUp]
        public virtual void SetUp()
        {
            Profiler.BeginSample("VS Tests SetUp");

            var prefs = TestPreferences.CreatePreferences();
            prefs.SetBoolNoEditorUpdate(BoolPref.ErrorOnRecursiveDispatch, false);
            prefs.SetBoolNoEditorUpdate(BoolPref.ErrorOnMultipleDispatchesPerFrame, false);

            m_Store = new Store(new TestState(default, prefs));
            StoreHelper.RegisterDefaultReducers(m_Store);

            if (CreateGraphOnStartup)
            {
                var graphAsset = GraphAssetCreationHelpers<TestGraphAssetModel>.CreateInMemoryGraphAsset(CreatedGraphType, "Test", k_GraphPath);
                m_Store.Dispatch(new LoadGraphAssetAction(graphAsset));
                AssumeIntegrity();
            }
            Profiler.EndSample();
        }

        [TearDown]
        public virtual void TearDown()
        {
            UnloadGraph();
            m_Store = null;
            Profiler.enabled = false;
        }

        void UnloadGraph()
        {
            State state = m_Store.State;

            if (state.GraphModel != null)
                AssetWatcher.Instance.UnwatchGraphAssetAtPath(state.GraphModel.AssetModel?.GetPath());

            state.UnloadCurrentGraphAsset();
        }

        protected void AssertIntegrity()
        {
            if (GraphModel != null)
                Assert.IsTrue(GraphModel.CheckIntegrity(Verbosity.Errors));
        }

        protected void AssumeIntegrity()
        {
            if (GraphModel != null)
                Assume.That(GraphModel.CheckIntegrity(Verbosity.Errors));
        }

        protected IEnumerable<NodeModel> GetAllNodes()
        {
            return GraphModel.NodeModels.Cast<NodeModel>();
        }

        protected NodeModel GetNode(int index)
        {
            return GetAllNodes().ElementAt(index);
        }

        protected IConstant GetConstantNode(int index)
        {
            return (GetAllNodes().ElementAt(index) as ConstantNodeModel)?.Value;
        }

        protected int GetNodeCount()
        {
            return GraphModel.NodeModels.Count;
        }

        protected IEnumerable<EdgeModel> GetAllEdges()
        {
            return GraphModel.EdgeModels.Cast<EdgeModel>();
        }

        protected EdgeModel GetEdge(int index)
        {
            return GetAllEdges().ElementAt(index);
        }

        protected int GetEdgeCount()
        {
            return GetAllEdges().Count();
        }

        protected IEnumerable<VariableDeclarationModel> GetAllVariableDeclarations()
        {
            return GraphModel.VariableDeclarations.Cast<VariableDeclarationModel>();
        }

        protected VariableDeclarationModel GetVariableDeclaration(int index)
        {
            return GetAllVariableDeclarations().ElementAt(index);
        }

        protected int GetVariableDeclarationCount()
        {
            return GetAllVariableDeclarations().Count();
        }

        protected IEnumerable<StickyNoteModel> GetAllStickyNotes()
        {
            return GraphModel.StickyNoteModels.Cast<StickyNoteModel>();
        }

        protected StickyNoteModel GetStickyNote(int index)
        {
            return GetAllStickyNotes().ElementAt(index);
        }

        protected int GetStickyNoteCount()
        {
            return GetAllStickyNotes().Count();
        }

        protected IEnumerable<PlacematModel> GetAllPlacemats()
        {
            return GraphModel.PlacematModels.Cast<PlacematModel>();
        }

        protected PlacematModel GetPlacemat(int index)
        {
            return GetAllPlacemats().ElementAt(index);
        }

        protected IVariableDeclarationModel GetGraphVariableDeclaration(string fieldName)
        {
            return GraphModel.VariableDeclarations.Single(f => f.DisplayTitle == fieldName);
        }

        protected void AddUsage(IVariableDeclarationModel fieldModel)
        {
            int prevCount = GetFloatingVariableModels(GraphModel).Count();
            m_Store.Dispatch(new CreateVariableNodesAction(fieldModel, Vector2.one));
            Assume.That(GetFloatingVariableModels(GraphModel).Count(), Is.EqualTo(prevCount + 1));
        }

        protected IVariableNodeModel GetGraphVariableUsage(string fieldName)
        {
            return GetFloatingVariableModels(GraphModel).First(f => f.Title == fieldName && f.GetVariableType() == VariableType.GraphVariable);
        }

        protected IVariableDeclarationModel CreateGraphVariableDeclaration(string fieldName, Type type)
        {
            int prevCount = GraphModel.VariableDeclarations.Count();

            m_Store.Dispatch(new CreateGraphVariableDeclarationAction(fieldName, false, type.GenerateTypeHandle()));

            Assert.AreEqual(prevCount + 1, GraphModel.VariableDeclarations.Count());
            IVariableDeclarationModel decl = GetGraphVariableDeclaration(fieldName);
            Assume.That(decl, Is.Not.Null);
            Assume.That(decl.DisplayTitle, Is.EqualTo(fieldName));
            return decl;
        }

        protected void EnableUndoRedoModificationsLogging()
        {
            m_Store.RegisterObserver(a => Debug.Log("Action " + a.GetType().Name));
            // TODO : Undo.postprocessModifications += PostprocessModifications;
        }

        public IEnumerable<VariableNodeModel> GetFloatingVariableModels(IGraphModel graphModel)
        {
            return graphModel.NodeModels.OfType<VariableNodeModel>().Where(v => !v.OutputPort.IsConnected());
        }

        public void RefreshReference<T>(ref T model) where T : class, INodeModel
        {
            model = GraphModel.NodesByGuid.TryGetValue(model.Guid, out var newOne) ? (T)newOne : model;
        }

        public void RefreshReference(ref IEdgeModel model)
        {
            var orig = model;

            model = orig == null ? null : GraphModel.EdgeModels.FirstOrDefault(e =>
            {
                return e?.ToNodeGuid == orig.ToNodeGuid && e.FromNodeGuid == orig.FromNodeGuid && e.ToPortId == orig.ToPortId && e.FromPortId == orig.FromPortId;
            });
        }

        public void RefreshReference(ref IStickyNoteModel model)
        {
            var orig = model;

            model = orig == null ? null : GraphModel.StickyNoteModels.FirstOrDefault(m => m.Guid == orig.Guid);
        }

        protected void DebugLogAllEdges()
        {
            foreach (var edgeModel in GraphModel.EdgeModels)
            {
                Debug.Log(((NodeModel)edgeModel.ToPort.NodeModel).Title + "<->" + ((NodeModel)edgeModel.FromPort.NodeModel).Title);
            }
        }
    }
}
