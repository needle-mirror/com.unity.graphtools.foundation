using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEditor.GraphToolsFoundation.Overdrive.Plugins.Debugging;
using UnityEngine;
using UnityEditor.GraphToolsFoundation.Searcher;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Base implementation for <see cref="IStencil"/>.
    /// </summary>
    public abstract class Stencil : IStencil
    {
        ITypeMetadataResolver m_TypeMetadataResolver;

        List<ITypeMetadata> m_AssembliesTypes;

        protected IToolbarProvider m_ToolbarProvider;
        protected ISearcherDatabaseProvider m_SearcherDatabaseProvider;

        protected DebugInstrumentationHandler m_DebugInstrumentationHandler;

        /// <inheritdoc />
        public IGraphModel GraphModel { get; set; }

        /// <inheritdoc />
        public abstract string ToolName { get; }

        public virtual IEnumerable<Type> EventTypes => Enumerable.Empty<Type>();

        /// <inheritdoc />
        public ITypeMetadataResolver TypeMetadataResolver => m_TypeMetadataResolver ??= new TypeMetadataResolver();

        public virtual IGraphProcessor CreateGraphProcessor()
        {
            return new NoOpGraphProcessor();
        }

        public virtual IToolbarProvider GetToolbarProvider()
        {
            return m_ToolbarProvider ??= new ToolbarProvider();
        }

        public virtual List<ITypeMetadata> GetAssembliesTypesMetadata()
        {
            if (m_AssembliesTypes != null)
                return m_AssembliesTypes;

            m_AssembliesTypes = new List<ITypeMetadata>();
            return m_AssembliesTypes;
        }

        [CanBeNull]
        public virtual ISearcherFilterProvider GetSearcherFilterProvider()
        {
            return null;
        }

        [CanBeNull]
        public virtual IGTFSearcherAdapter GetSearcherAdapter(IGraphModel graphModel, string title, IEnumerable<IPortModel> contextPortModel = null)
        {
            return new GraphNodeSearcherAdapter(graphModel, title);
        }

        public virtual ISearcherDatabaseProvider GetSearcherDatabaseProvider()
        {
            return m_SearcherDatabaseProvider ??= new DefaultSearcherDatabaseProvider(this);
        }

        public virtual void OnGraphProcessingStarted(IGraphModel graphModel) { }
        public virtual void OnGraphProcessingSucceeded(IGraphModel graphModel, GraphProcessingResult results) { }
        public virtual void OnGraphProcessingFailed(IGraphModel graphModel, GraphProcessingResult results) { }

        public virtual IEnumerable<IPluginHandler> GetGraphProcessingPluginHandlers(GraphProcessingOptions getGraphProcessingOptions)
        {
            if (getGraphProcessingOptions.HasFlag(GraphProcessingOptions.Tracing))
            {
                if (m_DebugInstrumentationHandler == null)
                    m_DebugInstrumentationHandler = new DebugInstrumentationHandler();

                yield return m_DebugInstrumentationHandler;
            }
        }

        public virtual bool RequiresInitialization(IVariableDeclarationModel decl) => decl.RequiresInitialization();

        /// <inheritdoc />
        public bool RequiresInspectorInitialization(IVariableDeclarationModel decl) => decl.RequiresInspectorInitialization();

        // PF: To preference
        public virtual bool MoveNodeDependenciesByDefault => false;

        public virtual IDebugger Debugger => null;

        /// <inheritdoc />
        public virtual Type GetConstantNodeValueType(TypeHandle typeHandle)
        {
            return null;
        }

        /// <inheritdoc />
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
                case PortDirection.Output:
                    SearcherService.ShowOutputToGraphNodes(this, commandDispatcher.State, portModel, worldPosition, item =>
                        commandDispatcher.Dispatch(new CreateNodeFromPortCommand(new[] { portModel }, localPosition, item, edgesToDelete)));
                    break;

                case PortDirection.Input:
                    SearcherService.ShowInputToGraphNodes(this, commandDispatcher.State, Enumerable.Repeat(portModel, 1), worldPosition, item =>
                        commandDispatcher.Dispatch(new CreateNodeFromPortCommand(new[] { portModel }, localPosition, item, edgesToDelete)));
                    break;
            }
        }

        public virtual void CreateNodesFromPort(CommandDispatcher commandDispatcher, IReadOnlyList<IPortModel> portModels, Vector2 localPosition, Vector2 worldPosition,
            IReadOnlyList<IEdgeModel> edgesToDelete)
        {
            switch (portModels.First().Direction)
            {
                case PortDirection.Output:
                    SearcherService.ShowOutputToGraphNodes(this, commandDispatcher.State, portModels, worldPosition, item =>
                        commandDispatcher.Dispatch(new CreateNodeFromPortCommand(portModels, localPosition, item, edgesToDelete)));
                    break;

                case PortDirection.Input:
                    SearcherService.ShowInputToGraphNodes(this, commandDispatcher.State, portModels, worldPosition, item =>
                        commandDispatcher.Dispatch(new CreateNodeFromPortCommand(portModels, localPosition, item, edgesToDelete)));
                    break;
            }
        }

        public virtual void PreProcessGraph(IGraphModel graphModel)
        {
        }

        /// <inheritdoc />
        public virtual IEnumerable<INodeModel> GetEntryPoints()
        {
            return Enumerable.Empty<INodeModel>();
        }

        /// <inheritdoc />
        public virtual bool CreateDependencyFromEdge(IEdgeModel edgeModel, out LinkedNodesDependency linkedNodesDependency, out INodeModel parentNodeModel)
        {
            linkedNodesDependency = new LinkedNodesDependency
            {
                DependentPort = edgeModel.FromPort,
                ParentPort = edgeModel.ToPort,
            };
            parentNodeModel = edgeModel.ToPort.NodeModel;

            return true;
        }

        /// <inheritdoc />
        public virtual IEnumerable<IEdgePortalModel> GetPortalDependencies(IEdgePortalModel portalModel)
        {
            if (portalModel is IEdgePortalEntryModel edgePortalModel)
            {
                return edgePortalModel.GraphModel.FindReferencesInGraph<IEdgePortalExitModel>(edgePortalModel.DeclarationModel);
            }

            return Enumerable.Empty<IEdgePortalModel>();
        }

        /// <inheritdoc />
        public virtual IEnumerable<IEdgePortalModel> GetLinkedPortals(IEdgePortalModel portalModel)
        {
            if (portalModel != null)
            {
                return portalModel.GraphModel.FindReferencesInGraph<IEdgePortalModel>(portalModel.DeclarationModel);
            }

            return Enumerable.Empty<IEdgePortalModel>();
        }

        public virtual void OnInspectorGUI()
        { }

        // PF FIXME: instead of having this virtual on the Stencil, tools (VS) should replace the command
        // handler for CreateVariableNodesCommand
        public virtual void OnDragAndDropVariableDeclarations(CommandDispatcher commandDispatcher, List<(IVariableDeclarationModel, SerializableGUID, Vector2)> variablesToCreate)
        {
            commandDispatcher.Dispatch(new CreateVariableNodesCommand(variablesToCreate));
        }

        /// <inheritdoc />
        public virtual bool GetPortCapacity(IPortModel portModel, out PortCapacity capacity)
        {
            capacity = default;
            return false;
        }

        /// <inheritdoc />
        public virtual bool CanPasteNode(INodeModel originalModel, IGraphModel graph) => true;

        public virtual string GetNodeDocumentation(SearcherItem node, IGraphElementModel model) =>
            null;

        /// <summary>
        /// Converts a <see cref="GraphProcessingError"/> to a <see cref="IGraphProcessingErrorModel"/>.
        /// </summary>
        /// <param name="error">The error to convert.</param>
        /// <returns>The converted error.</returns>
        public virtual IGraphProcessingErrorModel CreateProcessingErrorModel(GraphProcessingError error)
        {
            if (error.SourceNode != null && !error.SourceNode.Destroyed)
                return new GraphProcessingErrorModel(error);

            return null;
        }

        /// <inheritdoc />
        public abstract IBlackboardGraphModel CreateBlackboardGraphModel(IGraphAssetModel graphAssetModel);

        /// <summary>
        /// Populates the given <paramref name="menu"/> with the section to create variable declaration models for a blackboard.
        /// </summary>
        /// <param name="sectionName">The name of the section in which the menu is added.</param>
        /// <param name="menu">The menu to fill.</param>
        /// <param name="commandDispatcher">The command dispatcher tasked with dispatching the creation command.</param>
        public virtual void PopulateBlackboardCreateMenu(string sectionName, GenericMenu menu, CommandDispatcher commandDispatcher)
        {
            menu.AddItem(new GUIContent("Create Variable"), false, () =>
            {
                const string newItemName = "variable";
                var finalName = newItemName;
                var i = 0;
                // ReSharper disable once AccessToModifiedClosure
                while (commandDispatcher.State.WindowState.GraphModel.VariableDeclarations.Any(v => v.Title == finalName))
                    finalName = newItemName + i++;

                commandDispatcher.Dispatch(new CreateGraphVariableDeclarationCommand(finalName, true, TypeHandle.Float));
            });
        }
    }
}
