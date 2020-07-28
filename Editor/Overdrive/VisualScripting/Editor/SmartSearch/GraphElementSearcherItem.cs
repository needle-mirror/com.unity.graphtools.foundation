using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using UnityEditor.Searcher;
using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.GraphViewModel;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.SmartSearch
{
    public struct GraphNodeCreationData : IGraphNodeCreationData
    {
        public IGTFGraphModel GraphModel { get; }
        public Vector2 Position { get; }
        public SpawnFlags SpawnFlags { get; }
        public IReadOnlyList<GUID> Guids { get; }

        public GUID Guid => (Guids?.First()).GetValueOrDefault();

        public GraphNodeCreationData(IGTFGraphModel graphModel, Vector2 position, SpawnFlags spawnFlags = SpawnFlags.Default, IReadOnlyList<GUID> guids = null)
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
        public Func<GraphNodeCreationData, IGTFGraphElementModel[]> CreateElements { get; }
        public ISearcherItemData Data { get; }

        readonly Func<string> m_GetName;
        readonly string m_Name;

        public GraphNodeModelSearcherItem(
            ISearcherItemData data,
            Func<GraphNodeCreationData, IGTFGraphElementModel> createElement,
            Func<string> getName,
            List<SearcherItem> children = null,
            string help = null
        ) : this(data, createElement, getName(), children, getName, help)
        {
        }

        public GraphNodeModelSearcherItem(
            ISearcherItemData data,
            Func<GraphNodeCreationData, IGTFGraphElementModel> createElement,
            string name,
            List<SearcherItem> children = null,
            Func<string> getName = null,
            string help = null
        ) : base(name, children: children, help: help)
        {
            m_Name = name;
            m_GetName = getName;
            Data = data;
            CreateElements = d => new[] { createElement.Invoke(d) };
        }
    }
}
