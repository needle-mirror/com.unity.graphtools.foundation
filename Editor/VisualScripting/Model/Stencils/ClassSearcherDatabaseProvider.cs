using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Searcher;
using UnityEditor.VisualScripting.Editor;
using UnityEditor.VisualScripting.Editor.SmartSearch;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model;
using UnityEditor.VisualScripting.Model.Stencils;
using UnityEngine;
using UnityEngine.VisualScripting;

namespace UnityEditor.VisualScripting.Model.Stencils
{
    public class ClassSearcherDatabaseProvider : ISearcherDatabaseProvider
    {
        static readonly IEnumerable<Type> k_PredefinedSearcherTypes = new List<Type>
        {
            typeof(float),
            typeof(int),
            typeof(Math)
        };

        static readonly IEnumerable<Type> k_ConstantTypes = new List<Type>
        {
            typeof(bool),
            typeof(double),
            typeof(int),
            typeof(float),
            typeof(string),
            typeof(Enum),
            typeof(InputName),
            typeof(LayerName),
            typeof(TagName)
        };

        readonly Stencil m_Stencil;
        List<SearcherDatabase> m_GraphElementsSearcherDatabases;
        List<SearcherDatabase> m_ReferenceItemsSearcherDatabases;
        SearcherDatabase m_StaticTypesSearcherDatabase;
        List<ITypeMetadata> m_PrimitiveTypes;
        IEnumerable<Type> m_CustomTypes;
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
                    .AddEmptyFunction()
                    .AddStack()
                    .AddConstants(k_ConstantTypes)
                    .AddInlineExpression()
                    .AddUnaryOperators()
                    .AddBinaryOperators()
                    .AddControlFlows()
                    .AddMembers(GetCustomTypes(), MemberFlags.Method, BindingFlags.Static | BindingFlags.Public)
                    .AddMembers(
                        k_PredefinedSearcherTypes,
                        MemberFlags.Constructor | MemberFlags.Field | MemberFlags.Method | MemberFlags.Property | MemberFlags.Extension,
                        BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public
                    )
                    .AddMacros()
                    .Build()
            });
        }

        public virtual List<SearcherDatabase> GetReferenceItemsSearcherDatabases()
        {
            return m_ReferenceItemsSearcherDatabases ?? (m_ReferenceItemsSearcherDatabases = new List<SearcherDatabase>
            {
                new GraphElementSearcherDatabase(m_Stencil)
                    .AddGraphsMethods()
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

        public virtual List<SearcherDatabase> GetTypeMembersSearcherDatabases(TypeHandle typeHandle)
        {
            // TODO : Need to be handled by TypeHandle.Resolve
            Type type = typeHandle == TypeHandle.ThisType
                ? m_Stencil.GetThisType().Resolve(m_Stencil)
                : typeHandle.Resolve(m_Stencil);

            return new List<SearcherDatabase>
            {
                new GraphElementSearcherDatabase(m_Stencil)
                    .AddMembers(
                    new[] { type },
                    MemberFlags.Field | MemberFlags.Method | MemberFlags.Property | MemberFlags.Extension,
                    BindingFlags.Instance | BindingFlags.Public
                    )
                    .Build()
            };
        }

        public virtual List<SearcherDatabase> GetGraphVariablesSearcherDatabases(IGraphModel graphModel, IFunctionModel functionModel = null)
        {
            return new List<SearcherDatabase>
            {
                new GraphElementSearcherDatabase(m_Stencil)
                    .AddFunctionMembers(functionModel)
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

        public virtual void ClearReferenceItemsSearcherDatabases()
        {
            m_ReferenceItemsSearcherDatabases = null;
        }

        public virtual void ClearTypesItemsSearcherDatabases()
        {
            m_StaticTypesSearcherDatabase = null;
        }

        public virtual void ClearTypeMembersSearcherDatabases() {}

        public virtual void ClearGraphVariablesSearcherDatabases() {}

        IEnumerable<Type> GetCustomTypes()
        {
            return m_CustomTypes ?? (m_CustomTypes = TypeCache.GetTypesWithAttribute<NodeAttribute>()
                    .Where(t => !t.IsInterface)
                    .ToList());
        }
    }
}
