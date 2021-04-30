using System;
using System.Collections.Generic;
using UnityEditor.Searcher;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Graph node creation data used by the searcher.
    /// </summary>
    public struct GraphNodeCreationData : IGraphNodeCreationData
    {
        /// <summary>
        /// The interface to the graph where we want the node to be created in.
        /// </summary>
        public IGraphModel GraphModel { get; }

        /// <summary>
        /// The position at which the node should be created.
        /// </summary>
        public Vector2 Position { get; }

        /// <summary>
        /// The flags specifying how the node is to be spawned.
        /// </summary>
        public SpawnFlags SpawnFlags { get; }

        /// <summary>
        /// The SerializableGUID to assign to the newly created item.
        /// </summary>
        public SerializableGUID Guid { get; }

        /// <summary>
        /// Initializes a new GraphNodeCreationData.
        /// </summary>
        /// <param name="graphModel">The interface to the graph where we want the node to be created in.</param>
        /// <param name="position">The position at which the node should be created.</param>
        /// <param name="spawnFlags">The flags specifying how the node is to be spawned.</param>
        /// <param name="guid">The SerializableGUID to assign to the newly created item.</param>
        public GraphNodeCreationData(IGraphModel graphModel, Vector2 position, SpawnFlags spawnFlags = SpawnFlags.Default, SerializableGUID guid = default)
        {
            GraphModel = graphModel;
            Position = position;
            SpawnFlags = spawnFlags;
            Guid = guid;
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
            List<SearcherItem> children = null,
            string help = null
        ) : this(data, createElement, getName(), children, getName, help)
        {
        }

        public GraphNodeModelSearcherItem(
            ISearcherItemData data,
            Func<GraphNodeCreationData, IGraphElementModel> createElement,
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
