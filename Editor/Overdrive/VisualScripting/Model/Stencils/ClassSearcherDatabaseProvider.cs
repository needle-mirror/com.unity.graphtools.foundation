using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor.Searcher;
using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.SmartSearch;
using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.GraphViewModel;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive.VisualScripting;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting
{
    public class ClassSearcherDatabaseProvider : ISearcherDatabaseProvider
    {
        readonly Stencil m_Stencil;
        List<SearcherDatabase> m_GraphElementsSearcherDatabases;
        SearcherDatabase m_StaticTypesSearcherDatabase;
        List<ITypeMetadata> m_PrimitiveTypes;
        int m_AssetVersion = AssetWatcher.Version;
        int m_AssetModificationVersion = AssetModificationWatcher.Version;

        public ClassSearcherDatabaseProvider(Stencil stencil)
        {
            m_Stencil = stencil;
        }

        public virtual List<SearcherDatabase> GetGraphElementsSearcherDatabases()
        {
            if (AssetWatcher.Version != m_AssetVersion || AssetModificationWatcher.Version != m_AssetModificationVersion)
            {
                m_AssetVersion = AssetWatcher.Version;
                m_AssetModificationVersion = AssetModificationWatcher.Version;
                ClearGraphElementsSearcherDatabases();
            }

            return m_GraphElementsSearcherDatabases ?? (m_GraphElementsSearcherDatabases = new List<SearcherDatabase>
            {
                new GraphElementSearcherDatabase(m_Stencil)
                    .AddNodesWithSearcherItemAttribute()
                    .AddStickyNote()
                    .Build()
            });
        }

        public virtual List<SearcherDatabase> GetTypesSearcherDatabases()
        {
            return new List<SearcherDatabase>
            {
                m_StaticTypesSearcherDatabase ?? (m_StaticTypesSearcherDatabase = new TypeSearcherDatabase(m_Stencil, m_Stencil.GetAssembliesTypesMetadata())
                        .AddClasses()
                        .AddEnums()
                        .Build()),
                new TypeSearcherDatabase(m_Stencil, new List<ITypeMetadata>())
                    .Build()
            };
        }

        public virtual List<SearcherDatabase> GetGraphVariablesSearcherDatabases(IGraphModel graphModel)
        {
            return new List<SearcherDatabase>
            {
                new GraphElementSearcherDatabase(m_Stencil)
                    .AddGraphVariables(graphModel)
                    .Build()
            };
        }

        public virtual List<SearcherDatabase> GetDynamicSearcherDatabases(IPortModel portModel)
        {
            return new List<SearcherDatabase>();
        }

        public virtual void ClearGraphElementsSearcherDatabases()
        {
            m_GraphElementsSearcherDatabases = null;
        }

        public virtual void ClearTypesItemsSearcherDatabases()
        {
            m_StaticTypesSearcherDatabase = null;
        }

        public virtual void ClearTypeMembersSearcherDatabases() {}

        public virtual void ClearGraphVariablesSearcherDatabases() {}
    }
}
