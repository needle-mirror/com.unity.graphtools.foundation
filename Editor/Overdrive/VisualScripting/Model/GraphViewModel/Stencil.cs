using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.Plugins;
using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.SmartSearch;
using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.GraphViewModel;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive.VisualScripting;
using Object = UnityEngine.Object;
using UnityEditor.GraphToolsFoundation.Overdrive.GraphElements;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.Compilation;
using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.Translators;
using UnityEditor.Searcher;
using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.Interfaces;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting
{
    [Flags]
    [Serializable]
    [MovedFrom(false, "UnityEditor.VisualScripting.Model.Stencils", "Unity.GraphTools.Foundation.Overdrive.Editor")]
    public enum StencilCapabilityFlags
    {
        // ReSharper disable once ShiftExpressionRealShiftCountIsZero
        SupportsMacros = 1 << 0,
    }

    [PublicAPI]
    [Serializable]
    [MovedFrom(false, "UnityEditor.VisualScripting.Model.Stencils", "Unity.GraphTools.Foundation.Overdrive.Editor")]
    public abstract class Stencil
    {
        static readonly string[] k_BlackListedAssemblies =
        {
            "boo.lang",
            "castle.core",
            "excss.unity",
            "jetbrains",
            "lucene",
            "microsoft",
            "mono",
            "moq",
            "nunit",
            "system.web",
            "unityscript",
            "visualscriptingassembly-csharp"
        };

        static IEnumerable<Assembly> s_Assemblies;

        internal static IEnumerable<Assembly> CachedAssemblies
        {
            get
            {
                return s_Assemblies ?? (s_Assemblies = AppDomain.CurrentDomain.GetAssemblies()
                        .Where(a => !a.IsDynamic
                            && !k_BlackListedAssemblies.Any(b => a.GetName().Name.ToLower().Contains(b)))
                        .ToList());
            }
        }

        public virtual IEnumerable<Type> EventTypes => Enumerable.Empty<Type>();

        public bool RecompilationRequested { get; set; }

        List<ITypeMetadata> m_AssembliesTypes;

        protected IBlackboardProvider m_BlackboardProvider;
        protected IToolbarProvider m_ToolbarProvider;

        public bool addCreateAssetMenuAttribute;
        public string fileName = "";
        public string menuName = "";

        GraphContext m_GraphContext;

        public virtual IExternalDragNDropHandler DragNDropHandler => null;

        public GraphContext GraphContext => m_GraphContext ?? (m_GraphContext = CreateGraphContext());
        protected virtual GraphContext CreateGraphContext()
        {
            return new GraphContext();
        }

        public virtual ITranslator CreateTranslator()
        {
            return new NoOpTranslator();
        }

        public virtual TypeHandle GetThisType()
        {
            return typeof(object).GenerateTypeHandle(this);
        }

        public virtual Type GetBaseClass()
        {
            return typeof(object);
        }

        public virtual IVariableModel CreateVariableModelForDeclaration(IGraphModel graphModel, IVariableDeclarationModel declarationModel, Vector2 position, SpawnFlags spawnFlags = SpawnFlags.Default, GUID? guid = null)
        {
            if (declarationModel == null)
                return graphModel.CreateNode<ThisNodeModel>("this", position, spawnFlags, guid: guid);

            return graphModel.CreateNode<VariableNodeModel>(declarationModel.Title, position, spawnFlags, v => v.DeclarationModel = declarationModel, guid);
        }

        public virtual IBlackboardProvider GetBlackboardProvider()
        {
            return m_BlackboardProvider ?? (m_BlackboardProvider = new BlackboardProvider(this));
        }

        public virtual IToolbarProvider GetToolbarProvider()
        {
            return m_ToolbarProvider ?? (m_ToolbarProvider = new ToolbarProvider());
        }

        public virtual IEnumerable<Assembly> GetAssemblies()
        {
            return CachedAssemblies;
        }

        public virtual List<ITypeMetadata> GetAssembliesTypesMetadata()
        {
            if (m_AssembliesTypes != null)
                return m_AssembliesTypes;

            var types = GetAssemblies().SelectMany(a => a.GetTypesSafe()).ToList();
            m_AssembliesTypes = TaskUtility.RunTasks<Type, ITypeMetadata>(types, (type, cb) =>
            {
                if (!type.IsAbstract && !type.IsInterface && type.IsPublic
                    && !Attribute.IsDefined(type, typeof(ObsoleteAttribute)))
                    cb.Add(GenerateTypeHandle(type).GetMetadata(this));
            }).ToList();

            return m_AssembliesTypes;
        }

        [CanBeNull]
        public virtual ISearcherFilterProvider GetSearcherFilterProvider()
        {
            return null;
        }

        [CanBeNull]
        public virtual ISearcherAdapter GetSearcherAdapter(IGraphModel graphModel, string title, IPortModel contextPortModel = null)
        {
            return new GraphNodeSearcherAdapter(graphModel, title);
        }

        public abstract ISearcherDatabaseProvider GetSearcherDatabaseProvider();

        public virtual void OnCompilationSucceeded(VSGraphModel graphModel, CompilationResult results) {}

        public virtual void OnCompilationFailed(VSGraphModel graphModel, CompilationResult results) {}

        public virtual IEnumerable<IPluginHandler> GetCompilationPluginHandlers(CompilationOptions getCompilationOptions)
        {
            if (getCompilationOptions.HasFlag(CompilationOptions.Tracing))
                yield return new DebugInstrumentationHandler();
        }

        public virtual TypeHandle GenerateTypeHandle(Type t)
        {
            return CSharpTypeSerializer.GenerateTypeHandle(t);
        }

        public bool RequiresInitialization(IVariableDeclarationModel decl) => GraphContext.RequiresInitialization(decl);
        public bool RequiresInspectorInitialization(IVariableDeclarationModel decl) => GraphContext.RequiresInspectorInitialization(decl);

        public virtual StencilCapabilityFlags Capabilities => 0;
        public abstract IBuilder Builder { get; }
        public virtual bool MoveNodeDependenciesByDefault => true;

        public virtual string GetSourceFilePath(VSGraphModel graphModel)
        {
            return Path.Combine(ModelUtility.GetAssemblyRelativePath(), graphModel.TypeName + ".cs");
        }

        public virtual void RegisterReducers(Store store)
        {
        }

        static Dictionary<TypeHandle, Type> s_TypeToConstantNodeModelTypeCache;
        public virtual IDebugger Debugger => null;
        public virtual bool GeneratesCode => false;

        public virtual Type GetConstantNodeModelType(Type type)
        {
            return GetConstantNodeModelType(type.GenerateTypeHandle(this));
        }

        public virtual Type GetConstantNodeModelType(TypeHandle typeHandle)
        {
            if (s_TypeToConstantNodeModelTypeCache == null)
            {
                s_TypeToConstantNodeModelTypeCache = new Dictionary<TypeHandle, Type>
                {
                    { typeof(Boolean).GenerateTypeHandle(this), typeof(BooleanConstantNodeModel) },
                    { typeof(Color).GenerateTypeHandle(this), typeof(ColorConstantModel) },
                    { typeof(AnimationCurve).GenerateTypeHandle(this), typeof(CurveConstantNodeModel) },
                    { typeof(Double).GenerateTypeHandle(this), typeof(DoubleConstantModel) },
                    { typeof(Unknown).GenerateTypeHandle(this), typeof(EnumConstantNodeModel) },
                    { typeof(Single).GenerateTypeHandle(this), typeof(FloatConstantModel) },
                    { typeof(InputName).GenerateTypeHandle(this), typeof(InputConstantModel) },
                    { typeof(Int32).GenerateTypeHandle(this), typeof(IntConstantModel) },
                    { typeof(LayerName).GenerateTypeHandle(this), typeof(LayerConstantModel) },
                    { typeof(LayerMask).GenerateTypeHandle(this), typeof(LayerMaskConstantModel) },
                    { typeof(Object).GenerateTypeHandle(this), typeof(ObjectConstantModel) },
                    { typeof(Quaternion).GenerateTypeHandle(this), typeof(QuaternionConstantModel) },
                    { typeof(String).GenerateTypeHandle(this), typeof(StringConstantModel) },
                    { typeof(TagName).GenerateTypeHandle(this), typeof(TagConstantModel) },
                    { typeof(Vector2).GenerateTypeHandle(this), typeof(Vector2ConstantModel) },
                    { typeof(Vector3).GenerateTypeHandle(this), typeof(Vector3ConstantModel) },
                    { typeof(Vector4).GenerateTypeHandle(this), typeof(Vector4ConstantModel) },
                    { typeof(SceneAsset).GenerateTypeHandle(this), typeof(ConstantSceneAssetNodeModel) },
                };
            }

            if (s_TypeToConstantNodeModelTypeCache.TryGetValue(typeHandle, out var result))
                return result;

            Type t = typeHandle.Resolve(this);
            if (t.IsEnum || t == typeof(Enum))
                return typeof(EnumConstantNodeModel);

            return null;
        }

        public virtual void CreateNodesFromPort(Store store, IPortModel portModel, Vector2 localPosition, Vector2 worldPosition,
            IEnumerable<IGTFEdgeModel> edgesToDelete)
        {
            switch (portModel.Direction)
            {
                case Direction.Output:
                    SearcherService.ShowOutputToGraphNodes(store.GetState(), portModel, worldPosition, item =>
                        store.Dispatch(new CreateNodeFromOutputPortAction(portModel, localPosition, item, edgesToDelete)));
                    break;

                case Direction.Input:
                    SearcherService.ShowInputToGraphNodes(store.GetState(), portModel, worldPosition, item =>
                        store.Dispatch(new CreateNodeFromInputPortAction(portModel, localPosition, item, edgesToDelete)));
                    break;
            }
        }

        public virtual void PreProcessGraph(VSGraphModel graphModel)
        {
        }

        public virtual bool ValidateEdgeConnection(IPortModel inputPort, IPortModel outputPort)
        {
            return true;
        }

        public virtual IEnumerable<INodeModel> GetEntryPoints(VSGraphModel vsGraphModel)
        {
            return Enumerable.Empty<INodeModel>();
        }

        public virtual void OnInspectorGUI()
        {}

        public virtual bool CreateDependencyFromEdge(IEdgeModel model, out LinkedNodesDependency linkedNodesDependency, out INodeModel parent)
        {
            linkedNodesDependency = new LinkedNodesDependency
            {
                DependentPort = model.OutputPortModel,
                ParentPort = model.InputPortModel,
            };
            parent = model.InputPortModel.NodeModel;

            return true;
        }

        public virtual IEnumerable<IEdgePortalModel> GetPortalDependencies(IEdgePortalModel model)
        {
            if (model is IEdgePortalEntryModel)
            {
                return ((VariableDeclarationModel)model.DeclarationModel).FindReferencesInGraph()
                    .OfType<IEdgePortalExitModel>();
            }

            return Enumerable.Empty<IEdgePortalModel>();
        }

        public virtual void OnDragAndDropVariableDeclarations(Store store, List<Tuple<IVariableDeclarationModel, Vector2>> variablesToCreate)
        {
            store.Dispatch(new CreateVariableNodesAction(variablesToCreate));
        }

        /// <summary>
        /// Gets the port capacity of a port. This is called portModel?.GetDefaultCapacity() by NodeModel.GetPortCapacity(portModel)
        /// </summary>
        /// <param name="portModel"></param>
        /// <param name="capacity"></param>
        /// <returns></returns>
        public virtual bool GetPortCapacity(PortModel portModel, out PortCapacity capacity)
        {
            capacity = default;
            return false;
        }

        /// <summary>
        /// Used to skip some nodes when pasting/duplicating nodes
        /// </summary>
        /// <param name="originalModel"></param>
        /// <param name="graph"></param>
        /// <returns>If the node can be pasted/duplicated</returns>
        public virtual bool CanPasteNode(NodeModel originalModel, VSGraphModel graph) => true;
    }

    /// <summary>
    /// The trace of all recorded frames relevant to a specific graph and target tuple
    /// </summary>
    public interface IGraphTrace
    {
        IReadOnlyList<IFrameData> AllFrames { get; }
    }

    /// <summary>
    /// The trace of all steps recorded during a specific frame, in the context of a specific graph, target and frame
    /// </summary>
    public interface IFrameData
    {
        int Frame { get; }
        IEnumerable<TracingStep> GetDebuggingSteps(Stencil stencil);
    }

    /// <summary>
    /// Stencil specific implementation of tracing/debugging
    /// </summary>
    public interface IDebugger
    {
        /// <summary>
        /// Setup called when the tracing plugin is starting
        /// </summary>
        /// <param name="graphModel">The current graph model</param>
        /// <param name="tracingEnabled">The initial tracing state</param>
        void Start(IGraphModel graphModel, bool tracingEnabled);

        /// <summary>
        /// Tear down called when the tracing plugin is stopping
        /// </summary>
        void Stop();

        /// <summary>
        /// Gets collection of all debugging targets (entities, game objects, ...) as arbitrary indices
        /// </summary>
        /// <param name="graphModel">The current graph model</param>
        /// <returns>The list of targets or null if none could be produced</returns>
        IEnumerable<int> GetDebuggingTargets(IGraphModel graphModel);

        /// <summary>
        /// Used to fill the current tracing target label in the UI
        /// </summary>
        /// <param name="graphModel"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        string GetTargetLabel(IGraphModel graphModel, int target);

        /// <summary>
        /// Produces a list of steps for a given graph, frame and target
        /// </summary>
        /// <param name="currentGraphModel">The current graph</param>
        /// <param name="frame">The current frame</param>
        /// <param name="tracingTarget">The current target</param>
        /// <param name="stepList">The resulting list of steps</param>
        /// <returns>Returns true if successful</returns>
        bool GetTracingSteps(IGraphModel currentGraphModel, int frame, int tracingTarget, out List<TracingStep> stepList);

        /// <summary>
        /// Get the existing graph trace of a given graph and target including all recorded frames
        /// </summary>
        /// <param name="assetModelGraphModel">The current graph</param>
        /// <param name="currentTracingTarget">The current target</param>
        /// <returns>The trace of all frames relevant to this specific graph and target tuple</returns>
        IGraphTrace GetGraphTrace(IGraphModel assetModelGraphModel, int currentTracingTarget);

        /// <summary>
        /// Called when the tracing is toggle in a graph
        /// </summary>
        /// <param name="currentGraphModel"></param>
        /// <param name="enabled"></param>
        void OnToggleTracing(IGraphModel currentGraphModel, bool enabled);
    }
}
