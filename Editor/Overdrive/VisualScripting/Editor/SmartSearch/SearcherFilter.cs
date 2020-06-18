using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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

        public SearcherFilter WithEnums(Stencil stencil)
        {
            this.Register((Func<TypeSearcherItemData, bool>)(data => data.Type.GetMetadata(stencil).IsEnum));
            return this;
        }

        public SearcherFilter WithTypesInheriting<T>(Stencil stencil)
        {
            return WithTypesInheriting(stencil, typeof(T));
        }

        public SearcherFilter WithTypesInheriting<T, TA>(Stencil stencil) where TA : Attribute
        {
            return WithTypesInheriting(stencil, typeof(T), typeof(TA));
        }

        public SearcherFilter WithTypesInheriting(Stencil stencil, Type type, Type attributeType = null)
        {
            this.Register((Func<TypeSearcherItemData, bool>)(data =>
            {
                var dataType = data.Type.Resolve(stencil);
                return type.IsAssignableFrom(dataType) && (attributeType == null || dataType.GetCustomAttribute(attributeType) != null);
            }));
            return this;
        }

        public SearcherFilter WithMacros()
        {
            this.Register((Func<GraphAssetSearcherItemData, bool>)(data => data.GraphAssetModel != null));
            return this;
        }

        public SearcherFilter WithGraphAsset(IGraphAssetModel assetModel)
        {
            this.Register((Func<GraphAssetSearcherItemData, bool>)(data => data.GraphAssetModel == assetModel));
            return this;
        }

        public SearcherFilter WithVariables(Stencil stencil, IPortModel portModel)
        {
            Func<TypeSearcherItemData, bool> func = data =>
            {
                return portModel.DataTypeHandle == TypeHandle.Unknown
                    || portModel.DataTypeHandle.IsAssignableFrom(data.Type, stencil);
            };
            this.Register(func);
            return this;
        }

        public SearcherFilter WithVisualScriptingNodes()
        {
            this.Register((Func<NodeSearcherItemData, bool>)(data => data.Type != null));
            return this;
        }

        public SearcherFilter WithVisualScriptingNodes(Type type)
        {
            this.Register((Func<NodeSearcherItemData, bool>)(data => type.IsAssignableFrom(data.Type)));
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

        public SearcherFilter WithConstants(Stencil stencil, IPortModel portModel)
        {
            Func<TypeSearcherItemData, bool> func = data =>
            {
                if (!data.IsConstant)
                    return false;
                return portModel.DataTypeHandle == TypeHandle.Unknown
                    || portModel.DataTypeHandle.IsAssignableFrom(data.Type, stencil);
            };
            Register(func);
            return this;
        }

        public SearcherFilter WithConstants()
        {
            Register((Func<TypeSearcherItemData, bool>)(data => data.IsConstant));
            return this;
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
