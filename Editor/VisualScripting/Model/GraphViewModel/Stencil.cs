using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using Packages.VisualScripting.Editor.Helpers;
using UnityEditor.EditorCommon.Extensions;
using UnityEditor.VisualScripting.Editor;
using UnityEditor.VisualScripting.Editor.Plugins;
using UnityEditor.VisualScripting.Editor.SmartSearch;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model.Translators;
using UnityEditor.VisualScripting.Model.Compilation;
using UnityEngine;
using UnityEngine.VisualScripting;
using Object = UnityEngine.Object;
using UnityEditor.Experimental.GraphView;
using UnityEditor.Searcher;

namespace UnityEditor.VisualScripting.Model.Stencils
{
    [Flags]
    [Serializable]
    public enum StencilCapabilityFlags
    {
        SupportsMacros = 1 << 0,
    }

    [PublicAPI]
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

        public virtual IRuntimeStencilReference RuntimeReference => null;

        public bool RecompilationRequested { get; set; }

        List<ITypeMetadata> m_AssembliesTypes;

        protected IBlackboardProvider m_BlackboardProvider;

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
            return new RoslynTranslator(this);
        }

        public virtual TypeHandle GetThisType()
        {
            return typeof(object).GenerateTypeHandle(this);
        }

        public virtual Type GetBaseClass()
        {
            return typeof(object);
        }

        public virtual Type GetDefaultStackModelType()
        {
            return typeof(StackModel);
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
        public virtual ISearcherAdapter GetSearcherAdapter(IStackModel stackModel, string title)
        {
            return new StackNodeSearcherAdapter(stackModel, title);
        }

        [CanBeNull]
        public virtual ISearcherAdapter GetSearcherAdapter(IGraphModel graphModel, string title)
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
            return GraphContext.CSharpTypeSerializer.GenerateTypeHandle(t);
        }

        public bool RequiresInitialization(IVariableDeclarationModel decl) => GraphContext.RequiresInitialization(decl);
        public bool RequiresInspectorInitialization(IVariableDeclarationModel decl) => GraphContext.RequiresInspectorInitialization(decl);

        public virtual StencilCapabilityFlags Capabilities => 0;
        public abstract IBuilder Builder { get; }

        public virtual string GetSourceFilePath(VSGraphModel graphModel)
        {
            return Path.Combine(ModelUtility.GetAssemblyRelativePath(), graphModel.TypeName + ".cs");
        }

        public virtual void RegisterReducers(Store store)
        {
        }

        static Dictionary<TypeHandle, Type> s_TypeToConstantNodeModelTypeCache;

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

        public virtual void CreateNodesFromPort(Store store, IPortModel portModel, Vector2 position,
            IEnumerable<IEdgeModel> edgesToDelete, IStackModel stackModel, int index)
        {
            switch (portModel.PortType)
            {
                case PortType.Data:
                case PortType.Instance:
                    switch (portModel.Direction)
                    {
                        case Direction.Output when stackModel != null:
                            if (portModel.DataType != TypeHandle.Unknown)
                            {
                                SearcherService.ShowOutputToStackNodes(
                                    store.GetState(), stackModel, portModel, position, item =>
                                    {
                                        store.Dispatch(new CreateStackedNodeFromOutputPortAction(
                                            portModel, stackModel, index, item, edgesToDelete));
                                    });
                            }
                            break;

                        case Direction.Output:
                            SearcherService.ShowOutputToGraphNodes(store.GetState(), portModel, position, item =>
                                store.Dispatch(new CreateNodeFromOutputPortAction(portModel, position, item, edgesToDelete)));
                            break;

                        case Direction.Input:
                            SearcherService.ShowInputToGraphNodes(store.GetState(), portModel, position, item =>
                                store.Dispatch(new CreateNodeFromInputPortAction(portModel, position, item, edgesToDelete)));
                            break;

                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                    break;

                case PortType.Execution:
                    if (portModel.NodeModel is LoopStackModel loopStack && portModel.Direction == Direction.Input)
                    {
                        if (stackModel != null)
                        {
                            store.Dispatch(new CreateInsertLoopNodeAction(portModel, stackModel, index, loopStack, edgesToDelete));
                        }
                    }
                    else
                    {
                        store.Dispatch(new CreateNodeFromExecutionPortAction(portModel, position, edgesToDelete));
                    }
                    break;

                case PortType.Loop:
                    store.Dispatch(new CreateNodeFromLoopPortAction(portModel, position, edgesToDelete));
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

        public virtual IEnumerable<INodeModel> SpawnAllNodes(VSGraphModel vsGraphModel)
        {
            return Enumerable.Empty<INodeModel>();
        }

        public virtual IEnumerable<INodeModel> GetEntryPoints(VSGraphModel vsGraphModel)
        {
            return vsGraphModel.StackModels.OfType<IFunctionModel>().Where(x => x.IsEntryPoint && x.State == ModelState.Enabled);
        }

        public virtual void OnInspectorGUI()
        {}
    }
}
