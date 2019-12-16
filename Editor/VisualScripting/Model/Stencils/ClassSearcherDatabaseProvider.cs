using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor.Searcher;
using UnityEditor.VisualScripting.Editor;
using UnityEditor.VisualScripting.Editor.SmartSearch;
using UnityEditor.VisualScripting.GraphViewModel;
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
                    .AddConstants(k_ConstantTypes.Concat(GetCustomConstantVariables()))
                    .AddInlineExpression()
                    .AddUnaryOperators()
                    .AddBinaryOperators()
                    .AddControlFlows()
                    .AddMembers(
                        k_PredefinedSearcherTypes.Concat(GetCustomTypeMembers()),
                        MemberFlags.Constructor | MemberFlags.Field | MemberFlags.Method | MemberFlags.Property | MemberFlags.Extension,
                        BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public
                    )
                    .AddFields(GetCustomFields())
                    .AddProperties(GetCustomProperties())
                    .AddMethods(GetCustomMethods())
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

        protected IEnumerable<Type> GetCustomTypeMembers()
        {
            return TypeCache.GetTypesWithAttribute<NodeAttribute>()
                .Where(t =>
                {
                    if (t.IsInterface)
                        return false;

                    var attributes = t.GetCustomAttributes<NodeAttribute>();
                    return attributes.Any(attribute => attribute.StencilReferenceType == null
                        || attribute.StencilReferenceType.IsInstanceOfType(m_Stencil.RuntimeReference));
                })
                .Concat(TypeCache.GetMethodsWithAttribute<TypeMembersNodeAttribute>()
                    .Where(FilterMethodsByStencil<TypeMembersNodeAttribute>)
                    .SelectMany(m => (IEnumerable<Type>)m.Invoke(null, new object[] {}))
                    .Where(t => !t.IsInterface));
        }

        protected IEnumerable<MethodInfo> GetCustomMethods()
        {
            return TypeCache.GetMethodsWithAttribute<MethodNodeAttribute>()
                .Where(FilterMethodsByStencil<MethodNodeAttribute>)
                .SelectMany(m => (IEnumerable<MethodInfo>)m.Invoke(null, new object[] {}));
        }

        protected IEnumerable<PropertyInfo> GetCustomProperties()
        {
            return TypeCache.GetMethodsWithAttribute<PropertyNodeAttribute>()
                .Where(FilterMethodsByStencil<PropertyNodeAttribute>)
                .SelectMany(m => (IEnumerable<PropertyInfo>)m.Invoke(null, new object[] {}));
        }

        protected IEnumerable<FieldInfo> GetCustomFields()
        {
            return TypeCache.GetMethodsWithAttribute<FieldNodeAttribute>()
                .Where(FilterMethodsByStencil<FieldNodeAttribute>)
                .SelectMany(m => (IEnumerable<FieldInfo>)m.Invoke(null, new object[] {}));
        }

        protected IEnumerable<Type> GetCustomConstantVariables()
        {
            return TypeCache.GetMethodsWithAttribute<ConstantVariableNodeAttribute>()
                .Where(FilterMethodsByStencil<ConstantVariableNodeAttribute>)
                .SelectMany(m => (IEnumerable<Type>)m.Invoke(null, new object[] {}));
        }

        bool FilterMethodsByStencil<T>(MemberInfo methodInfo) where T : AbstractNodeAttribute
        {
            var attributes = methodInfo.GetCustomAttributes<T>();
            return attributes.Any(attribute => attribute.StencilReferenceType == null
                || attribute.StencilReferenceType.IsInstanceOfType(m_Stencil.RuntimeReference));
        }
    }
}
