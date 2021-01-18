using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.Plugins;
using UnityEngine;
using UnityEditor.Searcher;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    [PublicAPI]
    // Warning: Stencil is only serializable for backward compatibility purposes. It will stop being unserializable in the future
    [Serializable]
    public abstract class Stencil
    {
        public virtual IEnumerable<Type> EventTypes => Enumerable.Empty<Type>();

        List<ITypeMetadata> m_AssembliesTypes;

        protected IToolbarProvider m_ToolbarProvider;

        [SerializeReference]
        public IGraphModel GraphModel;

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
        public virtual ISearcherAdapter GetSearcherAdapter(IGraphModel graphModel, string title, IPortModel contextPortModel = null)
        {
            return new GraphNodeSearcherAdapter(graphModel, title);
        }

        public abstract ISearcherDatabaseProvider GetSearcherDatabaseProvider();
        public virtual void OnCompilationStarted(IGraphModel graphModel) {}
        public virtual void OnCompilationSucceeded(IGraphModel graphModel, CompilationResult results) {}
        public virtual void OnCompilationFailed(IGraphModel graphModel, CompilationResult results) {}

        public virtual IEnumerable<IPluginHandler> GetCompilationPluginHandlers(CompilationOptions getCompilationOptions)
        {
            if (getCompilationOptions.HasFlag(CompilationOptions.Tracing))
                yield return new DebugInstrumentationHandler();
        }

        public virtual bool RequiresInitialization(IVariableDeclarationModel decl) => GraphContext.RequiresInitialization(decl);
        public bool RequiresInspectorInitialization(IVariableDeclarationModel decl) => GraphContext.RequiresInspectorInitialization(decl);

        // PF: To preference
        public virtual bool MoveNodeDependenciesByDefault => true;

        public virtual IDebugger Debugger => null;

        public abstract Type GetConstantNodeValueType(TypeHandle typeHandle);

        public virtual IConstant CreateConstantValue(TypeHandle constantTypeHandle)
        {
            var nodeType = GetConstantNodeValueType(constantTypeHandle);
            var instance = (IConstant)Activator.CreateInstance(nodeType);
            instance.ObjectValue = instance.DefaultValue;
            return instance;
        }

        public virtual void CreateNodesFromPort(Store store, IPortModel portModel, Vector2 localPosition, Vector2 worldPosition,
            IReadOnlyList<IEdgeModel> edgesToDelete)
        {
            switch (portModel.Direction)
            {
                case Direction.Output:
                    SearcherService.ShowOutputToGraphNodes(store.State, portModel, worldPosition, item =>
                        store.Dispatch(new CreateNodeFromPortAction(portModel, localPosition, item, edgesToDelete)));
                    break;

                case Direction.Input:
                    SearcherService.ShowInputToGraphNodes(store.State, portModel, worldPosition, item =>
                        store.Dispatch(new CreateNodeFromPortAction(portModel, localPosition, item, edgesToDelete)));
                    break;
            }
        }

        public virtual void PreProcessGraph(IGraphModel graphModel)
        {
        }

        public virtual IEnumerable<INodeModel> GetEntryPoints(IGraphModel graphModel)
        {
            return Enumerable.Empty<INodeModel>();
        }

        public virtual void OnInspectorGUI()
        {}

        public virtual bool CreateDependencyFromEdge(IEdgeModel model, out LinkedNodesDependency linkedNodesDependency, out INodeModel parent)
        {
            linkedNodesDependency = new LinkedNodesDependency
            {
                DependentPort = model.FromPort,
                ParentPort = model.ToPort,
            };
            parent = model.ToPort.NodeModel;

            return true;
        }

        public virtual IEnumerable<IEdgePortalModel> GetPortalDependencies(IEdgePortalModel model)
        {
            if (model is IEdgePortalEntryModel edgePortalModel)
            {
                return edgePortalModel.GraphModel.FindReferencesInGraph<IEdgePortalExitModel>(edgePortalModel.DeclarationModel);
            }

            return Enumerable.Empty<IEdgePortalModel>();
        }

        public virtual IEnumerable<IEdgePortalModel> GetLinkedPortals(IEdgePortalModel model)
        {
            if (model is IEdgePortalModel edgePortalModel)
            {
                return edgePortalModel.GraphModel.FindReferencesInGraph<IEdgePortalModel>(edgePortalModel.DeclarationModel);
            }

            return Enumerable.Empty<IEdgePortalModel>();
        }

        public virtual void OnDragAndDropVariableDeclarations(Store store, List<(IVariableDeclarationModel, SerializableGUID, Vector2)> variablesToCreate)
        {
            store.Dispatch(new CreateVariableNodesAction(variablesToCreate));
        }

        /// <summary>
        /// Gets the port capacity of a port. This is called portModel?.GetDefaultCapacity() by NodeModel.GetPortCapacity(portModel)
        /// </summary>
        /// <param name="portModel"></param>
        /// <param name="capacity"></param>
        /// <returns></returns>
        public virtual bool GetPortCapacity(IPortModel portModel, out PortCapacity capacity)
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
        public virtual bool CanPasteNode(INodeModel originalModel, IGraphModel graph) => true;

        public virtual bool MigrateNode(INodeModel nodeModel, out INodeModel migrated)
        {
            migrated = null;
            return false;
        }

        public virtual string GetNodeDocumentation(SearcherItem node, IGraphElementModel model) =>
            null;
    }
}
