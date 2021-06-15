using System.Collections.Generic;
using UnityEditor.GraphToolsFoundation.Searcher;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// The default implementation of <see cref="ISearcherDatabaseProvider"/>.
    /// </summary>
    public class DefaultSearcherDatabaseProvider : ISearcherDatabaseProvider
    {
        Stencil m_Stencil;
        List<SearcherDatabaseBase> m_GraphElementsSearcherDatabases;

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultSearcherDatabaseProvider"/> class.
        /// </summary>
        /// <param name="stencil">The stencil.</param>
        public DefaultSearcherDatabaseProvider(Stencil stencil)
        {
            m_Stencil = stencil;
        }

        /// <inheritdoc />
        public virtual List<SearcherDatabaseBase> GetGraphElementsSearcherDatabases(IGraphModel graphModel)
        {
            return m_GraphElementsSearcherDatabases ??= new List<SearcherDatabaseBase>
            {
                new GraphElementSearcherDatabase(m_Stencil, graphModel)
                    .AddNodesWithSearcherItemAttribute()
                    .AddStickyNote()
                    .Build()
            };
        }

        /// <inheritdoc />
        public virtual List<SearcherDatabaseBase> GetVariableTypesSearcherDatabases()
        {
            return new List<SearcherDatabaseBase>();
        }

        /// <inheritdoc />
        public virtual List<SearcherDatabaseBase> GetGraphVariablesSearcherDatabases(IGraphModel graphModel)
        {
            return new List<SearcherDatabaseBase>();
        }

        /// <inheritdoc />
        public virtual List<SearcherDatabaseBase> GetDynamicSearcherDatabases(IPortModel portModel)
        {
            return new List<SearcherDatabaseBase>();
        }

        /// <inheritdoc />
        public virtual List<SearcherDatabaseBase> GetDynamicSearcherDatabases(IEnumerable<IPortModel> portModel)
        {
            return new List<SearcherDatabaseBase>();
        }
    }
}
