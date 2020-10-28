using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using Lucene.Net.Index;
using UnityEditor.Searcher;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    [PublicAPI]
    public class GraphElementSearcherDatabase
    {
        const string k_Constant = "Constant";
        const string k_Sticky = "Sticky Note";
        const string k_GraphVariables = "Graph Variables";

        // TODO: our builder methods ("AddStack",...) all use this field. Users should be able to create similar methods. making it public until we find a better solution
        public readonly List<SearcherItem> Items;
        public readonly Stencil Stencil;
        private IGraphModel m_GraphModel;

        public GraphElementSearcherDatabase(Stencil stencil, IGraphModel graphModel)
        {
            Stencil = stencil;
            Items = new List<SearcherItem>();
            m_GraphModel = graphModel;
        }

        private Dictionary<SearcherItem, IEnumerable<IIndexableField>> docs;
        public GraphElementSearcherDatabase AddNodesWithSearcherItemAttribute()
        {
            var types = TypeCache.GetTypesWithAttribute<SearcherItemAttribute>();
            foreach (var type in types)
            {
                var attributes = type.GetCustomAttributes<SearcherItemAttribute>().ToList();
                if (!attributes.Any())
                    continue;

                foreach (var attribute in attributes)
                {
                    if (!attribute.StencilType.IsInstanceOfType(Stencil))
                        continue;

                    var name = attribute.Path.Split('/').Last();
                    var path = attribute.Path.Remove(attribute.Path.LastIndexOf('/') + 1);

                    switch (attribute.Context)
                    {
                        case SearcherContext.Graph:
                        {
                            var node = new GraphNodeModelSearcherItem(
                                new NodeSearcherItemData(type),
                                data => data.CreateNode(type, name),
                                name
                            );

                            Items.AddAtPath(node, path);
                            break;
                        }

                        default:
                            Debug.LogWarning($"The node {type} is not a " +
                                $"{SearcherContext.Graph} node, so it cannot be add in the Searcher");
                            break;
                    }

                    break;
                }
            }

            return this;
        }

        private void IndexNodeAndGetDocumentation(GraphNodeModelSearcherItem node)
        {
            var model = Enumerable.FirstOrDefault(node.CreateElements(
                new GraphNodeCreationData(m_GraphModel, Vector2.zero, SpawnFlags.Orphan)));
            string help = Stencil.GetNodeDocumentation(node, model);
            node.Help = help;
            if (Stencil.GetSearcherDatabaseProvider() is IIndexableSearcherDatabaseProvider indexableSearcherDatabaseProvider)
            {
                var documentIndexer = new DocumentIndexer();
                if (indexableSearcherDatabaseProvider.Index(node, model, ref documentIndexer))
                {
                    if (docs == null)
                        docs = new Dictionary<SearcherItem, IEnumerable<IIndexableField>>();
                    if (documentIndexer.Document != null)
                        docs.Add(node, documentIndexer.Document);
                }
            }
        }

        public GraphElementSearcherDatabase AddStickyNote()
        {
            var node = new GraphNodeModelSearcherItem(
                new TagSearcherItemData(CommonSearcherTags.StickyNote),
                data =>
                {
                    var rect = new Rect(data.Position, StickyNote.defaultSize);
                    var graphModel = data.GraphModel;
                    return graphModel.CreateStickyNote(rect, data.SpawnFlags);
                },
                k_Sticky
            );
            Items.AddAtPath(node);

            return this;
        }

        public GraphElementSearcherDatabase AddConstants(IEnumerable<Type> types)
        {
            foreach (Type type in types)
            {
                AddConstants(type);
            }

            return this;
        }

        public GraphElementSearcherDatabase AddConstants(Type type)
        {
            TypeHandle handle = type.GenerateTypeHandle();

            SearcherItem parent = SearcherItemUtility.GetItemFromPath(Items, k_Constant);
            parent.AddChild(new GraphNodeModelSearcherItem(
                new TypeSearcherItemData(handle),
                data => data.CreateConstantNode("", handle),
                $"{type.FriendlyName().Nicify()} {k_Constant}"
            ));

            return this;
        }

        public GraphElementSearcherDatabase AddGraphVariables(IGraphModel graphModel)
        {
            SearcherItem parent = null;

            foreach (var declarationModel in graphModel.VariableDeclarations)
            {
                if (parent == null)
                {
                    parent = SearcherItemUtility.GetItemFromPath(Items, k_GraphVariables);
                }

                parent.AddChild(new GraphNodeModelSearcherItem(
                    new TypeSearcherItemData(declarationModel.DataType),
                    data => data.CreateVariableNode(declarationModel),
                    declarationModel.DisplayTitle
                ));
            }

            return this;
        }

        public LuceneSearcherDatabase Build()
        {
            Recurse(Items);

            return LuceneSearcherDatabase.Create(Items, docs);

            void Recurse(List<SearcherItem> searcherItems)
            {
                foreach (var searcherItem in searcherItems)
                {
                    if (searcherItem.HasChildren)
                        Recurse(searcherItem.Children);
                    if (searcherItem is GraphNodeModelSearcherItem graphNodeModelSearcherItem)
                        IndexNodeAndGetDocumentation(graphNodeModelSearcherItem);
                }
            }
        }
    }
}
