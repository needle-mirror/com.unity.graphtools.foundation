using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEditor.GraphToolsFoundation.Overdrive.Plugins.Debugging;
using UnityEngine;
using UnityEditor.Searcher;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    [PublicAPI]
    // Warning: Stencil is only serializable for backward compatibility purposes. It will stop being serializable in the future
    [Serializable]
    public abstract class Stencil
    {
        GraphContext m_GraphContext;

        List<ITypeMetadata> m_AssembliesTypes;

        protected IToolbarProvider m_ToolbarProvider;
        protected ISearcherDatabaseProvider m_SearcherDatabaseProvider;

        [SerializeReference]
        public IGraphModel GraphModel;

        /// <summary>
        /// The tool name, unique and human readable.
        /// </summary>
        public abstract string ToolName { get; }

        public virtual IEnumerable<Type> EventTypes => Enumerable.Empty<Type>();

        public GraphContext GraphContext => m_GraphContext ?? (m_GraphContext = CreateGraphContext());

        protected virtual GraphContext CreateGraphContext()
        {
            return new GraphContext();
        }

        public virtual IGraphProcessor CreateGraphProcessor()
        {
            return new NoOpGraphProcessor();
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
        public virtual ISearcherAdapter GetSearcherAdapter(IGraphModel graphModel, string title, IEnumerable<IPortModel> contextPortModel = null)
        {
            return new GraphNodeSearcherAdapter(graphModel, title);
        }

        public virtual ISearcherDatabaseProvider GetSearcherDatabaseProvider()
        {
            return m_SearcherDatabaseProvider ?? (m_SearcherDatabaseProvider = new DefaultSearcherDatabaseProvider(this));
        }

        public virtual void OnGraphProcessingStarted(IGraphModel graphModel) { }
        public virtual void OnGraphProcessingSucceeded(IGraphModel graphModel, GraphProcessingResult results) { }
        public virtual void OnGraphProcessingFailed(IGraphModel graphModel, GraphProcessingResult results) { }

        public virtual IEnumerable<IPluginHandler> GetGraphProcessingPluginHandlers(GraphProcessingOptions getGraphProcessingOptions)
        {
            if (getGraphProcessingOptions.HasFlag(GraphProcessingOptions.Tracing))
                yield return new DebugInstrumentationHandler();
        }

        public virtual bool RequiresInitialization(IVariableDeclarationModel decl) => GraphContext.RequiresInitialization(decl);
        public bool RequiresInspectorInitialization(IVariableDeclarationModel decl) => GraphContext.RequiresInspectorInitialization(decl);

        // PF: To preference
        public virtual bool MoveNodeDependenciesByDefault => false;

        public virtual IDebugger Debugger => null;

        public virtual Type GetConstantNodeValueType(TypeHandle typeHandle)
        {
            return null;
        }

        public virtual IConstant CreateConstantValue(TypeHandle constantTypeHandle)
        {
            var nodeType = GetConstantNodeValueType(constantTypeHandle);
            var instance = (IConstant)Activator.CreateInstance(nodeType);
            instance.ObjectValue = instance.DefaultValue;
            return instance;
        }

        public virtual void CreateNodesFromPort(CommandDispatcher commandDispatcher, IPortModel portModel, Vector2 localPosition, Vector2 worldPosition,
            IReadOnlyList<IEdgeModel> edgesToDelete)
        {
            switch (portModel.Direction)
            {
                case Direction.Output:
                    SearcherService.ShowOutputToGraphNodes(commandDispatcher.GraphToolState, portModel, worldPosition, item =>
                        commandDispatcher.Dispatch(new CreateNodeFromPortCommand(new[] { portModel }, localPosition, item, edgesToDelete)));
                    break;

                case Direction.Input:
                    SearcherService.ShowInputToGraphNodes(commandDispatcher.GraphToolState, Enumerable.Repeat(portModel, 1), worldPosition, item =>
                        commandDispatcher.Dispatch(new CreateNodeFromPortCommand(new[] { portModel }, localPosition, item, edgesToDelete)));
                    break;
            }
        }

        public virtual void CreateNodesFromPort(CommandDispatcher commandDispatcher, IReadOnlyList<IPortModel> portModels, Vector2 localPosition, Vector2 worldPosition,
            IReadOnlyList<IEdgeModel> edgesToDelete)
        {
            switch (portModels.First().Direction)
            {
                case Direction.Output:
                    SearcherService.ShowOutputToGraphNodes(commandDispatcher.GraphToolState, portModels, worldPosition, item =>
                        commandDispatcher.Dispatch(new CreateNodeFromPortCommand(portModels, localPosition, item, edgesToDelete)));
                    break;

                case Direction.Input:
                    SearcherService.ShowInputToGraphNodes(commandDispatcher.GraphToolState, portModels, worldPosition, item =>
                        commandDispatcher.Dispatch(new CreateNodeFromPortCommand(portModels, localPosition, item, edgesToDelete)));
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
        { }

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

        // PF FIXME: instead of having this virtual on the Stencil, tools (VS) should replace the command
        // handler for CreateVariableNodesCommand
        public virtual void OnDragAndDropVariableDeclarations(CommandDispatcher commandDispatcher, List<(IVariableDeclarationModel, SerializableGUID, Vector2)> variablesToCreate)
        {
            commandDispatcher.Dispatch(new CreateVariableNodesCommand(variablesToCreate));
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
        /// <returns>If the node can be pasted/duplicated.</returns>
        public virtual bool CanPasteNode(INodeModel originalModel, IGraphModel graph) => true;

        public virtual bool MigrateNode(INodeModel nodeModel, out INodeModel migrated)
        {
            migrated = null;
            return false;
        }

        public virtual string GetNodeDocumentation(SearcherItem node, IGraphElementModel model) =>
            null;

        /// <summary>
        /// Converts a <see cref="GraphProcessingError"/> to a <see cref="IGraphProcessingErrorModel"/>.
        /// </summary>
        /// <param name="error">The error to convert.</param>
        /// <returns>The converted error.</returns>
        public abstract IGraphProcessingErrorModel CreateProcessingErrorModel(GraphProcessingError error);
    }
}
