using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.Plugins;
using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.SmartSearch;
using UnityEngine;
using UnityEditor.GraphToolsFoundation.Overdrive.GraphElements;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting;
using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.Compilation;
using UnityEditor.Searcher;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    [PublicAPI]
    [Serializable]
    public abstract class Stencil
    {
        public virtual IEnumerable<Type> EventTypes => Enumerable.Empty<Type>();

        List<ITypeMetadata> m_AssembliesTypes;

        protected IBlackboardProvider m_BlackboardProvider;
        protected IToolbarProvider m_ToolbarProvider;

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
            return TypeHandle.Object;
        }

        public virtual IBlackboardProvider GetBlackboardProvider()
        {
            return m_BlackboardProvider ?? (m_BlackboardProvider = new BlackboardProvider(this));
        }

        public virtual IToolbarProvider GetToolbarProvider()
        {
            return m_ToolbarProvider ?? (m_ToolbarProvider = new ToolbarProvider());
        }

        public virtual List<ITypeMetadata> GetAssembliesTypesMetadata()
        {
            if (m_AssembliesTypes != null)
                return m_AssembliesTypes;

            var types = AssemblyCache.CachedAssemblies.SelectMany(a => a.GetTypesSafe()).ToList();
            m_AssembliesTypes = TaskUtility.RunTasks<Type, ITypeMetadata>(types, (type, cb) =>
            {
                if (!type.IsAbstract && !type.IsInterface && type.IsPublic
                    && !Attribute.IsDefined(type, typeof(ObsoleteAttribute)))
                    cb.Add(TypeSerializer.GenerateTypeHandle(type).GetMetadata(this));
            }).ToList();

            return m_AssembliesTypes;
        }

        [CanBeNull]
        public virtual ISearcherFilterProvider GetSearcherFilterProvider()
        {
            return null;
        }

        [CanBeNull]
        public virtual ISearcherAdapter GetSearcherAdapter(IGTFGraphModel graphModel, string title, IGTFPortModel contextPortModel = null)
        {
            return new GraphNodeSearcherAdapter(graphModel, title);
        }

        public abstract ISearcherDatabaseProvider GetSearcherDatabaseProvider();
        public virtual void OnCompilationStarted(IGTFGraphModel graphModel) {}
        public virtual void OnCompilationSucceeded(IGTFGraphModel graphModel, CompilationResult results) {}
        public virtual void OnCompilationFailed(IGTFGraphModel graphModel, CompilationResult results) {}

        public virtual IEnumerable<IPluginHandler> GetCompilationPluginHandlers(CompilationOptions getCompilationOptions)
        {
            if (getCompilationOptions.HasFlag(CompilationOptions.Tracing))
                yield return new DebugInstrumentationHandler();
        }

        public bool RequiresInitialization(IGTFVariableDeclarationModel decl) => GraphContext.RequiresInitialization(decl);
        public bool RequiresInspectorInitialization(IGTFVariableDeclarationModel decl) => GraphContext.RequiresInspectorInitialization(decl);

        public abstract IBuilder Builder { get; }

        // PF: To preference
        public virtual bool MoveNodeDependenciesByDefault => true;

        static Dictionary<TypeHandle, Type> s_TypeToConstantNodeModelTypeCache;
        public virtual IDebugger Debugger => null;

        public virtual Type GetConstantNodeValueType(TypeHandle typeHandle)
        {
            if (s_TypeToConstantNodeModelTypeCache == null)
            {
                s_TypeToConstantNodeModelTypeCache = new Dictionary<TypeHandle, Type>
                {
                    { typeof(Boolean).GenerateTypeHandle(), typeof(BooleanConstant) },
                    { typeof(Color).GenerateTypeHandle(), typeof(ColorConstant) },
                    { typeof(Double).GenerateTypeHandle(), typeof(DoubleConstant) },
                    { typeof(Single).GenerateTypeHandle(), typeof(FloatConstant) },
                    { typeof(InputName).GenerateTypeHandle(), typeof(InputConstant) },
                    { typeof(Int32).GenerateTypeHandle(), typeof(IntConstant) },
                    { typeof(Quaternion).GenerateTypeHandle(), typeof(QuaternionConstant) },
                    { typeof(String).GenerateTypeHandle(), typeof(StringConstant) },
                    { typeof(Vector2).GenerateTypeHandle(), typeof(Vector2Constant) },
                    { typeof(Vector3).GenerateTypeHandle(), typeof(Vector3Constant) },
                    { typeof(Vector4).GenerateTypeHandle(), typeof(Vector4Constant) },
                };
            }

            if (s_TypeToConstantNodeModelTypeCache.TryGetValue(typeHandle, out var result))
                return result;

            Type t = typeHandle.Resolve();
            if (t.IsEnum || t == typeof(Enum))
                return typeof(EnumConstant);

            return null;
        }

        public virtual void CreateNodesFromPort(Store store, IGTFPortModel portModel, Vector2 localPosition, Vector2 worldPosition,
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

        public virtual void PreProcessGraph(IGTFGraphModel graphModel)
        {
        }

        public virtual IEnumerable<IGTFNodeModel> GetEntryPoints(IGTFGraphModel graphModel)
        {
            return Enumerable.Empty<IGTFNodeModel>();
        }

        public virtual void OnInspectorGUI()
        {}

        public virtual bool CreateDependencyFromEdge(IGTFEdgeModel model, out LinkedNodesDependency linkedNodesDependency, out IGTFNodeModel parent)
        {
            linkedNodesDependency = new LinkedNodesDependency
            {
                DependentPort = model.FromPort,
                ParentPort = model.ToPort,
            };
            parent = model.ToPort.NodeModel;

            return true;
        }

        public virtual IEnumerable<IGTFEdgePortalModel> GetPortalDependencies(IGTFEdgePortalModel model)
        {
            if (model is IGTFEdgePortalEntryModel edgePortalModel)
            {
                return edgePortalModel.GraphModel.FindReferencesInGraph<IGTFEdgePortalExitModel>(edgePortalModel.DeclarationModel);
            }

            return Enumerable.Empty<IGTFEdgePortalModel>();
        }

        public virtual IEnumerable<IGTFEdgePortalModel> GetLinkedPortals(IGTFEdgePortalModel model)
        {
            if (model is IGTFEdgePortalModel edgePortalModel)
            {
                return edgePortalModel.GraphModel.FindReferencesInGraph<IGTFEdgePortalModel>(edgePortalModel.DeclarationModel);
            }

            return Enumerable.Empty<IGTFEdgePortalModel>();
        }

        public virtual void OnDragAndDropVariableDeclarations(Store store, List<(IGTFVariableDeclarationModel, SerializableGUID, Vector2)> variablesToCreate)
        {
            store.Dispatch(new CreateVariableNodesAction(variablesToCreate));
        }

        /// <summary>
        /// Gets the port capacity of a port. This is called portModel?.GetDefaultCapacity() by NodeModel.GetPortCapacity(portModel)
        /// </summary>
        /// <param name="portModel"></param>
        /// <param name="capacity"></param>
        /// <returns></returns>
        public virtual bool GetPortCapacity(IGTFPortModel portModel, out PortCapacity capacity)
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
        public virtual bool CanPasteNode(IGTFNodeModel originalModel, IGTFGraphModel graph) => true;

        public virtual bool MigrateNode(IGTFNodeModel nodeModel, out IGTFNodeModel migrated)
        {
            migrated = null;
            return false;
        }

        public virtual string GetNodeDocumentation(SearcherItem node, IGTFGraphElementModel model) =>
            null;
    }
}
