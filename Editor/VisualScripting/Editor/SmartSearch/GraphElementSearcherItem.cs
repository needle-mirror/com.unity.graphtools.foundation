using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Searcher;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEngine;

namespace UnityEditor.VisualScripting.Editor.SmartSearch
{
    public struct StackNodeCreationData : IStackedNodeCreationData
    {
        public IStackModel StackModel { get; }
        public int Index { get; }
        public SpawnFlags SpawnFlags { get; }
        public IReadOnlyList<GUID> Guids { get; }

        public GUID? GuidAt(int index)
        {
            if (Guids != null && index < Guids.Count)
                return Guids[index];
            return null;
        }

        public GUID? Guid => Guids?.First();

        public StackNodeCreationData(IStackModel stackModel, int index,
                                     SpawnFlags spawnFlags = SpawnFlags.Default, IReadOnlyList<GUID> guids = null)
        {
            StackModel = stackModel;
            Index = index;
            SpawnFlags = spawnFlags;
            Guids = guids;
        }
    }

    public struct GraphNodeCreationData : IGraphNodeCreationData
    {
        public IGraphModel GraphModel { get; }
        public Vector2 Position { get; }
        public SpawnFlags SpawnFlags { get; }
        public IReadOnlyList<GUID> Guids { get; }

        public GUID? Guid => Guids?.First();

        public GraphNodeCreationData(IGraphModel graphModel, Vector2 position, SpawnFlags spawnFlags = SpawnFlags.Default, IReadOnlyList<GUID> guids = null)
        {
            GraphModel = graphModel;
            Position = position;
            SpawnFlags = spawnFlags;
            Guids = guids;
        }
    }

    public class GraphNodeModelSearcherItem : SearcherItem, ISearcherItemDataProvider
    {
        public override string Name => m_GetName != null ? m_GetName.Invoke() : m_Name;
        public Func<GraphNodeCreationData, IGraphElementModel[]> CreateElements { get; }
        public ISearcherItemData Data { get; }

        readonly Func<string> m_GetName;
        readonly string m_Name;

        public GraphNodeModelSearcherItem(
            ISearcherItemData data,
            Func<GraphNodeCreationData, IGraphElementModel> createElement,
            Func<string> getName,
            string help = "",
            List<SearcherItem> children = null
        ) : this(data, createElement, getName(), help, children)
        {
            m_GetName = getName;
        }

        public GraphNodeModelSearcherItem(
            ISearcherItemData data,
            Func<GraphNodeCreationData, IGraphElementModel> createElement,
            string name,
            string help = "",
            List<SearcherItem> children = null
        ) : base(name, help, children)
        {
            m_Name = name;
            Data = data;
            CreateElements = d => new[] { createElement.Invoke(d) };
        }
    }

    public class StackNodeModelSearcherItem : SearcherItem, ISearcherItemDataProvider
    {
        public override string Name => m_GetName != null ? m_GetName.Invoke() : m_Name;
        public Func<StackNodeCreationData, IGraphElementModel[]> CreateElements { get; }
        public ISearcherItemData Data { get; }

        readonly Func<string> m_GetName;
        readonly string m_Name;

        public StackNodeModelSearcherItem(
            ISearcherItemData data,
            Func<StackNodeCreationData, IGraphElementModel[]> createElements,
            string name,
            string help = "",
            List<SearcherItem> children = null
        ) : base(name, help, children)
        {
            m_Name = name;
            Data = data;
            CreateElements = createElements;
        }

        public StackNodeModelSearcherItem(
            ISearcherItemData data,
            Func<StackNodeCreationData, IGraphElementModel> createElement,
            string name,
            string help = "",
            List<SearcherItem> children = null
        ) : this(data, d => new[] { createElement.Invoke(d) }, name, help, children)
        {
        }

        public StackNodeModelSearcherItem(
            ISearcherItemData data,
            Func<StackNodeCreationData, IGraphElementModel> createElement,
            Func<string> getName,
            string help = "",
            List<SearcherItem> children = null
        ) : this(data, createElement, getName(), help, children)
        {
            m_GetName = getName;
        }

        public StackNodeModelSearcherItem(
            ISearcherItemData data,
            Func<StackNodeCreationData, IGraphElementModel[]> createElement,
            Func<string> getName,
            string help = "",
            List<SearcherItem> children = null
        ) : this(data, createElement, getName(), help, children)
        {
            m_GetName = getName;
        }
    }
}
