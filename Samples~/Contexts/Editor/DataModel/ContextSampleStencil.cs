using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.GraphToolsFoundation.Overdrive;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEditor.Searcher;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.Contexts
{
    class ContextSampleStencil : Stencil, ISearcherDatabaseProvider
    {
        SearcherDatabase m_Database;

        List<SearcherDatabase> m_Databases = new List<SearcherDatabase>();

        List<SearcherDatabaseBase> m_BaseDatabases = new List<SearcherDatabaseBase>();

        public override string ToolName
        {
            get { return GraphName; }
        }
        public static string GraphName
        {
            get { return "Contexts"; }
        }

        IGraphElementModel CreateElement(GraphNodeCreationData data, Type nodeType)
        {
            IGraphElementModel model = System.Activator.CreateInstance(nodeType) as IGraphElementModel;

            return model;
        }

        public ContextSampleStencil()
        {
            List<SearcherItem> itemList = new List<SearcherItem>();
            itemList.Add(new GraphNodeModelSearcherItem(null, t => NodeDataCreationExtensions.CreateNode(t, typeof(SampleContext)), "Context"));
            itemList.Add(new GraphNodeModelSearcherItem(null, t => NodeDataCreationExtensions.CreateNode(t, typeof(SampleContextVertical)), "Context with vertical"));

            m_Database = new SearcherDatabase(itemList.ToArray());
            m_Databases.Add(m_Database);
            m_BaseDatabases.Add(m_Database);

            SetSearcherSize(SearcherService.Usage.k_CreateNode, new Vector2(500, 400), 2.25f);
        }

        /// <inheritdoc />
        public override IBlackboardGraphModel CreateBlackboardGraphModel(IGraphAssetModel graphAssetModel)
        {
            return new BlackboardGraphModel(graphAssetModel);
        }

        public override Type GetConstantNodeValueType(TypeHandle typeHandle)
        {
            return TypeToConstantMapper.GetConstantNodeType(typeHandle);
        }

        public override ISearcherDatabaseProvider GetSearcherDatabaseProvider()
        {
            return this;
        }

        List<SearcherDatabaseBase> ISearcherDatabaseProvider.GetGraphElementsSearcherDatabases(IGraphModel graphModel)
        {
            return m_BaseDatabases;
        }

        List<SearcherDatabase> m_EmptyList = new List<SearcherDatabase>();
        List<SearcherDatabase> ISearcherDatabaseProvider.GetVariableTypesSearcherDatabases()
        {
            return m_EmptyList;
        }

        List<SearcherDatabaseBase> ISearcherDatabaseProvider.GetGraphVariablesSearcherDatabases(IGraphModel graphModel)
        {
            return m_BaseDatabases;
        }

        List<SearcherDatabaseBase> ISearcherDatabaseProvider.GetDynamicSearcherDatabases(IPortModel portModel)
        {
            return m_BaseDatabases;
        }

        public List<SearcherDatabaseBase> GetDynamicSearcherDatabases(IEnumerable<IPortModel> portModel)
        {
            return m_BaseDatabases;
        }
    }
}
