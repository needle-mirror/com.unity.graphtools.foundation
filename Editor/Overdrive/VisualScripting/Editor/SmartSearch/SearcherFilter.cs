using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.GraphViewModel;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.SmartSearch
{
    public class SearcherFilter
    {
        public static SearcherFilter Empty { get; } = new SearcherFilter(SearcherContext.None);

        readonly SearcherContext m_Context;
        readonly List<Func<ISearcherItemData, bool>> m_Filters;

        public SearcherFilter(SearcherContext context)
        {
            m_Context = context;
            m_Filters = new List<Func<ISearcherItemData, bool>>();
        }

        public SearcherFilter WithTypesInheriting<T>()
        {
            return WithTypesInheriting(typeof(T));
        }

        public SearcherFilter WithTypesInheriting<T, TA>() where TA : Attribute
        {
            return WithTypesInheriting(typeof(T), typeof(TA));
        }

        public SearcherFilter WithTypesInheriting(Type type, Type attributeType = null)
        {
            this.Register((Func<TypeSearcherItemData, bool>)(data =>
            {
                var dataType = data.Type.Resolve();
                return type.IsAssignableFrom(dataType) && (attributeType == null || dataType.GetCustomAttribute(attributeType) != null);
            }));
            return this;
        }

        public SearcherFilter WithVisualScriptingNodes()
        {
            this.Register((Func<NodeSearcherItemData, bool>)(data => data.Type != null));
            return this;
        }

        public SearcherFilter WithVisualScriptingNodesExcept(IEnumerable<Type> exceptions)
        {
            Register((Func<NodeSearcherItemData, bool>)(data => data.Type != null && !exceptions.Any(e => e.IsAssignableFrom(data.Type))));
            return this;
        }

        public SearcherFilter WithTag(CommonSearcherTags tag)
        {
            Register<ITaggedSearcherTag>(data => data.Tag == tag);
            return this;
        }

        public SearcherFilter WithTags(IEnumerable<CommonSearcherTags> tags)
        {
            Register<ITaggedSearcherTag>(data => tags.Contains(data.Tag));
            return this;
        }

        public SearcherFilter WithStickyNote()
        {
            return WithTag(CommonSearcherTags.StickyNote);
        }

        public void Register(Func<ISearcherItemData, bool> filter)
        {
            m_Filters.Add(filter);
        }

        public void Register<T>(Func<T, bool> filter) where T : ISearcherItemData
        {
            Register(data => data is T itemData && filter.Invoke(itemData));
        }

        public bool ApplyFilters(ISearcherItemData data)
        {
            return m_Filters.Count == 0 || m_Filters.Any(f => f.Invoke(data));
        }
    }
}
