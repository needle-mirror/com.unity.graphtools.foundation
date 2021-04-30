using System;
using System.Collections.Generic;
using UnityEditor.Searcher;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests
{
    public class ClassSearcherDatabaseProvider : ISearcherDatabaseProvider
    {
        readonly Stencil m_Stencil;
        List<SearcherDatabaseBase> m_GraphElementsSearcherDatabases;
        SearcherDatabase m_StaticTypesSearcherDatabase;
        int m_AssetModificationVersion = AssetModificationWatcher.Version;

        public ClassSearcherDatabaseProvider(Stencil stencil)
        {
            m_Stencil = stencil;
        }

        public virtual List<SearcherDatabaseBase> GetGraphElementsSearcherDatabases(IGraphModel graphModel)
        {
            if (AssetModificationWatcher.Version != m_AssetModificationVersion)
            {
                m_AssetModificationVersion = AssetModificationWatcher.Version;
                ClearGraphElementsSearcherDatabases();
            }

            return m_GraphElementsSearcherDatabases ?? (m_GraphElementsSearcherDatabases = new List<SearcherDatabaseBase>
            {
                new GraphElementSearcherDatabase(m_Stencil, graphModel)
                    .AddNodesWithSearcherItemAttribute()
                    .AddStickyNote()
                    .Build()
            });
        }

        public virtual List<SearcherDatabase> GetVariableTypesSearcherDatabases()
        {
            return new List<SearcherDatabase>
            {
                m_StaticTypesSearcherDatabase ?? (m_StaticTypesSearcherDatabase = TypeSearcherDatabase.FromTypes(m_Stencil, new Type[] {typeof(float), typeof(bool)}))
            };
        }

        public virtual List<SearcherDatabaseBase> GetGraphVariablesSearcherDatabases(IGraphModel graphModel)
        {
            return new List<SearcherDatabaseBase>
            {
                new GraphElementSearcherDatabase(m_Stencil, graphModel)
                    .AddGraphVariables(graphModel)
                    .Build()
            };
        }

        public virtual List<SearcherDatabaseBase> GetDynamicSearcherDatabases(IPortModel portModel)
        {
            return new List<SearcherDatabaseBase>();
        }

        public virtual List<SearcherDatabaseBase> GetDynamicSearcherDatabases(IEnumerable<IPortModel> portModel)
        {
            return new List<SearcherDatabaseBase>();
        }

        public virtual void ClearGraphElementsSearcherDatabases()
        {
            m_GraphElementsSearcherDatabases = null;
        }

        public virtual void ClearTypesItemsSearcherDatabases()
        {
            m_StaticTypesSearcherDatabase = null;
        }

        public virtual void ClearTypeMembersSearcherDatabases() { }

        public virtual void ClearGraphVariablesSearcherDatabases() { }
    }
}
