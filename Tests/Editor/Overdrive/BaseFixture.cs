using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.GraphElements;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting;
using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.GraphViewModel;
using UnityEngine;
using UnityEngine.Profiling;

// ReSharper disable AccessToStaticMemberViaDerivedType

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests
{
    public enum TestingMode { Action, UndoRedo }

    class TestEditorDataModel : IEditorDataModel
    {
        public UpdateFlags UpdateFlags { get; private set; }

        List<IGTFGraphElementModel> m_ModelsToUpdate = new List<IGTFGraphElementModel>();

        public IEnumerable<IGTFGraphElementModel> ModelsToUpdate => m_ModelsToUpdate;

        public void AddModelToUpdate(IGTFGraphElementModel controller)
        {
            m_ModelsToUpdate.Add(controller);
        }

        public void ClearModelsToUpdate()
        {
            m_ModelsToUpdate.Clear();
        }

        public IGTFGraphElementModel ElementModelToRename { get; set; }
        public GUID NodeToFrameGuid { get; set; } = default;
        public int CurrentGraphIndex => 0;
        public Preferences Preferences { get; }  = CreatePreferences();

        static Preferences CreatePreferences()
        {
            var prefs = VSPreferences.CreatePreferences();
            prefs.SetBoolNoEditorUpdate(BoolPref.ErrorOnRecursiveDispatch, false);
            prefs.SetBoolNoEditorUpdate(BoolPref.ErrorOnMultipleDispatchesPerFrame, false);
            return prefs;
        }

        public GameObject BoundObject { get; set; }

        public List<OpenedGraph> PreviousGraphModels { get; } = new List<OpenedGraph>();
        public int UpdateCounter { get; set; }
        public bool TracingEnabled { get; set; }
        public bool CompilationPending { get; set; }

        public void SetUpdateFlag(UpdateFlags flag)
        {
            UpdateFlags = flag;
        }

        public void RequestCompilation(RequestCompilationOptions options)
        {
            throw new NotImplementedException();
        }

        public bool ShouldSelectElementUponCreation(IGraphElement hasGraphElementModel)
        {
            return false;
        }

        public void SelectElementsUponCreation(IEnumerable<IGTFGraphElementModel> graphElementModels, bool select)
        {
        }

        public void ClearElementsToSelectUponCreation()
        {
        }

        public IPluginRepository PluginRepository { get; } = new TestPluginRepository();

        class TestPluginRepository : IPluginRepository
        {
            public void RegisterPlugins(IEnumerable<IPluginHandler> plugins) {}
            public void UnregisterPlugins(IEnumerable<IPluginHandler> except = null) {}
            public IEnumerable<IPluginHandler> RegisteredPlugins { get; }
        }
    }

    class TestState : Overdrive.VisualScripting.State
    {
        public TestState(IEditorDataModel dataModel)
            : base(dataModel)
        {
        }

        public TestState()
            : this(new TestEditorDataModel())
        {
        }
    }

    public class TestStore : Overdrive.VisualScripting.Store
    {
        public TestStore(Overdrive.VisualScripting.State initialState)
            : base(initialState)
        {
            Undo.undoRedoPerformed += UndoRedoPerformed;
        }

        public override void Dispose()
        {
            // ReSharper disable once DelegateSubtraction
            Undo.undoRedoPerformed -= UndoRedoPerformed;
        }

        void UndoRedoPerformed()
        {
            Profiler.BeginSample("TestStore_UndoRedo");
            InvokeStateChanged();
            Profiler.EndSample();
        }

        public override void Dispatch<TAction>(TAction action)
        {
            base.Dispatch(action);
            Update();
            InvokeStateChanged();
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

    [Category("VSEditor")]
    [PublicAPI]
    public abstract class BaseFixture
    {
        protected TestStore m_Store;
        protected const string k_GraphPath = "Assets/test.asset";

        protected GraphModel GraphModel => (GraphModel)m_Store.GetState().CurrentGraphModel;
        protected Stencil Stencil => GraphModel.Stencil;

        protected abstract bool CreateGraphOnStartup { get; }
        protected virtual bool WriteOnDisk => false;
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

        protected void TestPrereqActionPostreq<T>(TestingMode mode, Action checkReqs, Func<T> provideAction, Action checkPostReqs) where T : IAction
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

        static void CheckUndo<T>(Action checkReqs, Func<T> provideAction) where T : IAction
        {
            AssertPreviousTest(checkReqs);
            AssertPreviousTest(() => provideAction());
        }

        protected void TestPrereqActionPostreq<T>(TestingMode mode, Func<T> checkReqsAndProvideAction, Action checkPostReqs) where T : IAction
        {
            TestPrereqActionPostreq(mode, () => {}, checkReqsAndProvideAction, checkPostReqs);
        }

        [SetUp]
        public virtual void SetUp()
        {
            Profiler.BeginSample("VS Tests SetUp");
            if (WriteOnDisk)
                AssetDatabase.DeleteAsset(k_GraphPath);
            m_Store = new TestStore(new TestState());

            m_Store.StateChanged += AssertIntegrity;

            if (CreateGraphOnStartup)
            {
                m_Store.Dispatch(new CreateGraphAssetAction(CreatedGraphType, typeof(TestGraphAssetModel), "Test", k_GraphPath, writeOnDisk: WriteOnDisk));
                AssumeIntegrity();
            }
            Profiler.EndSample();
        }

        [TearDown]
        public virtual void TearDown()
        {
            m_Store.StateChanged -= AssertIntegrity;
            m_Store.Dispatch(new UnloadGraphAssetAction());
            m_Store.Dispose();
            Profiler.enabled = false;
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
            return (GetAllNodes().ElementAt(index) as ConstantNodeModel).Value;
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

        protected IGTFVariableDeclarationModel GetGraphVariableDeclaration(string fieldName)
        {
            return GraphModel.VariableDeclarations.Single(f => f.DisplayTitle == fieldName);
        }

        protected void AddUsage(IVariableDeclarationModel fieldModel)
        {
            int prevCount = GetFloatingVariableModels(GraphModel).Count();
            m_Store.Dispatch(new CreateVariableNodesAction(fieldModel, Vector2.one));
            Assume.That(GetFloatingVariableModels(GraphModel).Count(), Is.EqualTo(prevCount + 1));
        }

        protected IGTFVariableNodeModel GetGraphVariableUsage(string fieldName)
        {
            return GetFloatingVariableModels(GraphModel).First(f => f.Title == fieldName && f.VariableType == VariableType.GraphVariable);
        }

        protected IGTFVariableDeclarationModel CreateGraphVariableDeclaration(string fieldName, Type type)
        {
            int prevCount = GraphModel.VariableDeclarations.Count();

            m_Store.Dispatch(new CreateGraphVariableDeclarationAction(fieldName, false, type.GenerateTypeHandle()));

            Assert.AreEqual(prevCount + 1, GraphModel.VariableDeclarations.Count());
            IGTFVariableDeclarationModel decl = GetGraphVariableDeclaration(fieldName);
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
            return graphModel.NodeModels.OfType<VariableNodeModel>().Where(v => !v.OutputPort.IsConnected);
        }

        public void RefreshReference<T>(ref T model) where T : class, IGTFNodeModel
        {
            model = GraphModel.NodesByGuid.TryGetValue(model.Guid, out var newOne) ? (T)newOne : model;
        }

        public void RefreshReference(ref IGTFEdgeModel model)
        {
            var orig = model as IEdgeModel;

            model = orig == null ? null : GraphModel.EdgeModels.FirstOrDefault(ee =>
            {
                var e = ee as IEdgeModel;
                return e.ToNodeGuid == orig.ToNodeGuid && e.FromNodeGuid == orig.FromNodeGuid && e.ToPortId == orig.ToPortId && e.FromPortId == orig.FromPortId;
            });
        }

        public void RefreshReference(ref IGTFStickyNoteModel model)
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

        /*private static UndoPropertyModification[] PostprocessModifications(UndoPropertyModification[] modifications)
        {
            Func<PropertyModification, string> f = mod =>
                {
                    var o = mod.objectReference is SemanticGraph ? "graph" : mod.objectReference == null ? mod.value : mod.objectReference.GetType().Name;
                    return $"{mod.target}: {o}\n";
                };
            StringBuilder sb = new StringBuilder();
            foreach (var mod in modifications)
            {
                sb.AppendLine($"{mod.previousValue.propertyPath}:\n{f(mod.previousValue)}\n{f(mod.currentValue)}");
            }
            Debug.Log("POSTPROCESSMODIFICATIONS\n" + sb);
            return modifications;
        }*/
    }
}
