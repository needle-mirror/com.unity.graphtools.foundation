using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.GraphElements;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting;
using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.Plugins;
using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.GraphViewModel;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.GraphToolsFoundation.Overdrive.VisualScripting;
using State = UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.State;

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

        public IGraphElementModel ElementModelToRename { get; set; }
        public GUID NodeToFrameGuid { get; set; } = default;
        public int CurrentGraphIndex => 0;
        public VSPreferences Preferences { get; }  = new VSPreferences();

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

        public bool ShouldSelectElementUponCreation(IHasGraphElementModel hasGraphElementModel)
        {
            return false;
        }

        public void SelectElementsUponCreation(IEnumerable<IGraphElementModel> graphElementModels, bool select)
        {
        }

        public void ClearElementsToSelectUponCreation()
        {
        }

        public bool ShouldExpandBlackboardRowUponCreation(string rowName)
        {
            return false;
        }

        public void ExpandBlackboardRowsUponCreation(IEnumerable<string> rowNames, bool expand)
        {
        }

        public bool ShouldActivateElementUponCreation(IHasGraphElementModel hasGraphElementModel)
        {
            return false;
        }

        public void ActivateElementsUponCreation(IEnumerable<IGraphElementModel> graphElementModels, bool activate)
        {
        }

        public bool ShouldExpandElementUponCreation(IVisualScriptingField visualScriptingField)
        {
            return false;
        }

        public void ExpandElementsUponCreation(IEnumerable<IVisualScriptingField> visualScriptingFields, bool expand)
        {
        }

        public IPluginRepository PluginRepository { get; } = new TestPluginRepository();

        class TestPluginRepository : IPluginRepository
        {
            public void RegisterPlugins(CompilationOptions getCompilationOptions) {}
            public void UnregisterPlugins(IEnumerable<IPluginHandler> except = null) {}
            public IEnumerable<IPluginHandler> RegisteredPlugins { get; }
        }
    }

    class TestState : State
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

    public class TestStore : Store
    {
        public TestStore(State initialState)
            : base(initialState, Options.TrackUndoRedo)
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

        protected VSGraphModel GraphModel => ((VSGraphModel)m_Store.GetState().CurrentGraphModel);
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
                m_Store.Dispatch(new CreateGraphAssetAction(CreatedGraphType, "Test", k_GraphPath, writeOnDisk: WriteOnDisk));
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
                Assert.IsTrue(GraphModel.CheckIntegrity(GraphToolsFoundation.Overdrive.VisualScripting.GraphViewModel.GraphModel.Verbosity.Errors));
        }

        protected void AssumeIntegrity()
        {
            if (GraphModel != null)
                Assume.That(GraphModel.CheckIntegrity(GraphToolsFoundation.Overdrive.VisualScripting.GraphViewModel.GraphModel.Verbosity.Errors));
        }

        protected IEnumerable<NodeModel> GetAllNodes()
        {
            return GraphModel.NodeModels.Cast<NodeModel>();
        }

        protected NodeModel GetNode(int index)
        {
            return GetAllNodes().ElementAt(index);
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
            return GraphModel.GraphVariableModels.Cast<VariableDeclarationModel>();
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
            return GraphModel.GraphVariableModels.Single(f => f.Title == fieldName);
        }

        protected void AddUsage(IVariableDeclarationModel fieldModel)
        {
            int prevCount = GetFloatingVariableModels(GraphModel).Count();
            m_Store.Dispatch(new CreateVariableNodesAction(fieldModel, Vector2.one));
            Assume.That(GetFloatingVariableModels(GraphModel).Count(), Is.EqualTo(prevCount + 1));
        }

        protected IVariableModel GetGraphVariableUsage(string fieldName)
        {
            return GetFloatingVariableModels(GraphModel).First(f => f.Title == fieldName && f.VariableType == VariableType.GraphVariable);
        }

        protected IVariableDeclarationModel CreateGraphVariableDeclaration(string fieldName, Type type)
        {
            int prevCount = GraphModel.GraphVariableModels.Count();

            m_Store.Dispatch(new CreateGraphVariableDeclarationAction(fieldName, false, type.GenerateTypeHandle(Stencil)));

            Assert.AreEqual(prevCount + 1, GraphModel.GraphVariableModels.Count());
            IVariableDeclarationModel decl = GetGraphVariableDeclaration(fieldName);
            Assume.That(decl, Is.Not.Null);
            Assume.That(decl.Title, Is.EqualTo(fieldName));
            return decl;
        }

        protected void EnableUndoRedoModificationsLogging()
        {
            m_Store.Register(a => Debug.Log("Action " + a.GetType().Name));
            // TODO : Undo.postprocessModifications += PostprocessModifications;
        }

        public IEnumerable<VariableNodeModel> GetFloatingVariableModels(IGraphModel graphModel)
        {
            return graphModel.NodeModels.OfType<VariableNodeModel>().Where(v => !v.OutputPort.IsConnected);
        }

        public void RefreshReference<T>(ref T model) where T : class, INodeModel
        {
            model = GraphModel.NodesByGuid.TryGetValue(model.Guid, out var newOne) ? (T)newOne : model;
        }

        public void RefreshReference(ref IEdgeModel model)
        {
            var orig = model;

            model = orig == null ? null : GraphModel.EdgeModels.FirstOrDefault(e => e.InputNodeGuid == orig.InputNodeGuid && e.OutputNodeGuid == orig.OutputNodeGuid && e.InputId == orig.InputId && e.OutputId == orig.OutputId);
        }

        public void RefreshReference(ref IStickyNoteModel model)
        {
            var orig = model;

            model = orig == null ? null : GraphModel.StickyNoteModels.FirstOrDefault(m => m.GetId() == orig.GetId());
        }

        protected void DebugLogAllEdges()
        {
            foreach (var edgeModel in GraphModel.EdgeModels)
            {
                Debug.Log(((NodeModel)edgeModel.InputPortModel.NodeModel).Title + "<->" + ((NodeModel)edgeModel.OutputPortModel.NodeModel).Title);
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
