using System.Collections.Generic;
using UnityEditor.Searcher;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public class DefaultSearcherDatabaseProvider : ISearcherDatabaseProvider
    {
        Stencil m_Stencil;
        List<SearcherDatabaseBase> m_GraphElementsSearcherDatabases;

        public DefaultSearcherDatabaseProvider(Stencil stencil)
        {
            m_Stencil = stencil;
        }

        public virtual List<SearcherDatabaseBase> GetGraphElementsSearcherDatabases(IGraphModel graphModel)
        {
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
            return new List<SearcherDatabase>();
        }

        public virtual List<SearcherDatabaseBase> GetGraphVariablesSearcherDatabases(IGraphModel graphModel)
        {
            return new List<SearcherDatabaseBase>();
        }

        public virtual List<SearcherDatabaseBase> GetDynamicSearcherDatabases(IPortModel portModel)
        {
            return new List<SearcherDatabaseBase>();
        }

        public virtual List<SearcherDatabaseBase> GetDynamicSearcherDatabases(IEnumerable<IPortModel> portModel)
        {
            return new List<SearcherDatabaseBase>();
        }
    }
}
