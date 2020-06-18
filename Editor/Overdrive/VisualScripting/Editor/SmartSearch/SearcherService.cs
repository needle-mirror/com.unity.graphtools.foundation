using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Searcher;
using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.GraphViewModel;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.SmartSearch
{
    public static class SearcherService
    {
        public static SearcherGraphView GraphView { get; }

        static readonly SearcherWindow.Alignment k_FindAlignment = new SearcherWindow.Alignment(
            SearcherWindow.Alignment.Vertical.Top, SearcherWindow.Alignment.Horizontal.Center);
        static readonly TypeSearcherAdapter k_TypeAdapter =  new TypeSearcherAdapter("Pick a type");
        static readonly Comparison<SearcherItem> k_GraphElementSort = (x, y) =>
        {
            var xRoot = GetRoot(x);
            var yRoot = GetRoot(y);

            if (xRoot == yRoot)
                return x.Id - y.Id;

            if (xRoot.HasChildren == yRoot.HasChildren)
                return string.Compare(xRoot.Name, yRoot.Name, StringComparison.Ordinal);

            return xRoot.HasChildren ? 1 : -1;
        };

        public static readonly Comparison<SearcherItem> TypeComparison = (x, y) =>
        {
            var xRoot = GetRoot(x);
            var yRoot = GetRoot(y);

            if (xRoot == yRoot)
                return x.Id - y.Id;

            if (xRoot.HasChildren == yRoot.HasChildren)
            {
                const string lastItemName = "Advanced";

                if (xRoot.Name == lastItemName) return 1;
                if (yRoot.Name == lastItemName) return -1;
                return string.Compare(xRoot.Name, yRoot.Name, StringComparison.Ordinal);
            }

            return xRoot.HasChildren ? 1 : -1;
        };

        static SearcherService()
        {
            GraphView = new SearcherGraphView(new Store(new State(new VSEditorDataModel(null))));
        }

        public static void ShowInputToGraphNodes(State state, IPortModel portModel, Vector2 position,
            Action<GraphNodeModelSearcherItem> callback)
        {
            var stencil = state.CurrentGraphModel.Stencil;
            var filter = stencil.GetSearcherFilterProvider()?.GetInputToGraphSearcherFilter(portModel);
            var adapter = stencil.GetSearcherAdapter(state.CurrentGraphModel, "Add an input node", portModel);
            var dbProvider = stencil.GetSearcherDatabaseProvider();
            var dbs = dbProvider.GetGraphElementsSearcherDatabases()
                .Concat(dbProvider.GetGraphVariablesSearcherDatabases(state.CurrentGraphModel))
                .Concat(dbProvider.GetDynamicSearcherDatabases(portModel))
                .ToList();

            PromptSearcher(dbs, filter, adapter, position, callback);
        }

        public static void ShowOutputToGraphNodes(State state, IPortModel portModel, Vector2 position,
            Action<GraphNodeModelSearcherItem> callback)
        {
            var stencil = state.CurrentGraphModel.Stencil;
            var filter = stencil.GetSearcherFilterProvider()?.GetOutputToGraphSearcherFilter(portModel);
            var adapter = stencil.GetSearcherAdapter(state.CurrentGraphModel, $"Choose an action for {portModel.DataTypeHandle.GetMetadata(stencil).FriendlyName}", portModel);
            var dbProvider = stencil.GetSearcherDatabaseProvider();
            var dbs = dbProvider.GetGraphElementsSearcherDatabases().ToList();

            PromptSearcher(dbs, filter, adapter, position, callback);
        }

        public static void ShowEdgeNodes(State state, IEdgeModel edgeModel, Vector2 position,
            Action<GraphNodeModelSearcherItem> callback)
        {
            var stencil = state.CurrentGraphModel.Stencil;
            var filter = stencil.GetSearcherFilterProvider()?.GetEdgeSearcherFilter(edgeModel);
            var adapter = stencil.GetSearcherAdapter(state.CurrentGraphModel, "Insert Node");
            var dbProvider = stencil.GetSearcherDatabaseProvider();
            var dbs = dbProvider.GetGraphElementsSearcherDatabases().ToList();

            PromptSearcher(dbs, filter, adapter, position, callback);
        }

        public static void ShowGraphNodes(State state, Vector2 position, Action<GraphNodeModelSearcherItem> callback)
        {
            var stencil = state.CurrentGraphModel.Stencil;
            var filter = stencil.GetSearcherFilterProvider()?.GetGraphSearcherFilter();
            var adapter = stencil.GetSearcherAdapter(state.CurrentGraphModel, "Add a graph node");
            var dbProvider = stencil.GetSearcherDatabaseProvider();
            var dbs = dbProvider.GetGraphElementsSearcherDatabases()
                .Concat(dbProvider.GetDynamicSearcherDatabases(null))
                .ToList();

            PromptSearcher(dbs, filter, adapter, position, callback);
        }

        static void PromptSearcher<T>(List<SearcherDatabase> databases, SearcherFilter filter,
            ISearcherAdapter adapter, Vector2 position, Action<T> callback) where T : ISearcherItemDataProvider
        {
            ApplyDatabasesFilter<T>(databases, filter);
            var searcher = new Searcher.Searcher(databases, adapter) { SortComparison = k_GraphElementSort };

            SearcherWindow.Show(EditorWindow.focusedWindow, searcher, item =>
            {
                if (item is T dataProvider)
                {
                    callback(dataProvider);
                    return true;
                }

                return false;
            }, position, null);
        }

        static SearcherItem GetRoot(SearcherItem item)
        {
            if (item.Parent == null)
                return item;

            SearcherItem parent = item.Parent;
            while (true)
            {
                if (parent.Parent == null)
                    break;

                parent = parent.Parent;
            }

            return parent;
        }

        static void ApplyDatabasesFilter<T>(IEnumerable<SearcherDatabase> databases, SearcherFilter filter)
            where T : ISearcherItemDataProvider
        {
            foreach (var database in databases)
            {
                database.MatchFilter = (query, item) =>
                {
                    if (!(item is T dataProvider))
                        return false;

                    if (filter == null || filter == SearcherFilter.Empty)
                        return true;

                    return filter.ApplyFilters(dataProvider.Data);
                };
            }
        }

        // Used to display data that is not meant to be persisted. The database will be overwritten after each call to SearcherWindow.Show(...).
        internal static void ShowTransientData(EditorWindow host, IEnumerable<SearcherItem> items,
            ISearcherAdapter adapter, Action<SearcherItem> selectionDelegate, Vector2 pos)
        {
            var database = SearcherDatabase.Create(items.ToList(), "", false);
            var searcher = new Searcher.Searcher(database, adapter);

            SearcherWindow.Show(host, searcher, x =>
            {
                host.Focus();
                selectionDelegate(x);

                return !(Event.current?.modifiers.HasFlag(EventModifiers.Control)).GetValueOrDefault();
            }, pos, null);
        }

        internal static void FindInGraph(
            EditorWindow host,
            VSGraphModel graph,
            Action<FindInGraphAdapter.FindSearcherItem> highlightDelegate,
            Action<FindInGraphAdapter.FindSearcherItem> selectionDelegate
        )
        {
            var items = graph.GetAllNodes()
                .Where(x => !string.IsNullOrEmpty(x.Title))
                .Select(MakeFindItems)
                .ToList();
            var database = SearcherDatabase.Create(items, "", false);
            var searcher = new Searcher.Searcher(database, new FindInGraphAdapter(highlightDelegate));
            var position = new Vector2(host.rootVisualElement.layout.center.x, 0);

            SearcherWindow.Show(host, searcher, item =>
            {
                selectionDelegate(item as FindInGraphAdapter.FindSearcherItem);
                return true;
            },
                position, null, k_FindAlignment);
        }

        internal static void ShowEnumValues(string title, Type enumType, Vector2 position, Action<Enum, int> callback)
        {
            var items = Enum.GetValues(enumType)
                .Cast<Enum>()
                .Select(v => new EnumValuesAdapter.EnumValueSearcherItem(v) as SearcherItem)
                .ToList();
            var database = SearcherDatabase.Create(items, "", false);
            var searcher = new Searcher.Searcher(database, new EnumValuesAdapter(title));

            SearcherWindow.Show(EditorWindow.focusedWindow, searcher, item =>
            {
                if (item == null)
                    return false;

                callback(((EnumValuesAdapter.EnumValueSearcherItem)item).value, 0);
                return true;
            }, position, null);
        }

        public static void ShowValues(string title, IEnumerable<string> values, Vector2 position,
            Action<string, int> callback)
        {
            var items = values.Select(v => new SearcherItem(v)).ToList();
            var database = SearcherDatabase.Create(items, "", false);
            var searcher = new Searcher.Searcher(database, new SimpleSearcherAdapter(title));

            SearcherWindow.Show(EditorWindow.focusedWindow, searcher, item =>
            {
                if (item == null)
                    return false;

                callback(item.Name, item.Id);
                return true;
            }, position, null);
        }

        public static void ShowTypes(Stencil stencil, Vector2 position, Action<TypeHandle, int> callback,
            SearcherFilter userFilter = null)
        {
            var databases = stencil.GetSearcherDatabaseProvider().GetTypesSearcherDatabases();
            foreach (var database in databases)
            {
                database.MatchFilter = (query, item) =>
                {
                    if (!(item is TypeSearcherItem typeItem))
                        return false;

                    var filter = stencil.GetSearcherFilterProvider()?.GetTypeSearcherFilter();
                    var res = true;

                    if (filter != null)
                        res &= filter.ApplyFilters(typeItem.Data);

                    if (userFilter != null)
                        res &= userFilter.ApplyFilters(typeItem.Data);

                    return res;
                };
            }

            var searcher = new Searcher.Searcher(databases, k_TypeAdapter) { SortComparison = TypeComparison };
            SearcherWindow.Show(EditorWindow.focusedWindow, searcher, item =>
            {
                if (!(item is TypeSearcherItem typeItem))
                    return false;

                callback(typeItem.Type, 0);
                return true;
            }, position, null);
        }

        static SearcherItem MakeFindItems(INodeModel node)
        {
            List<SearcherItem> children = null;
            string title = node.Title;

            switch (node)
            {
                // TODO virtual property in NodeModel formatting what's displayed in the find window
                case IConstantNodeModel _:
                {
                    var nodeTitle = node is StringConstantModel ? $"\"{node.Title}\"" : node.Title;
                    title = $"Const {((ConstantNodeModel)node).Type.Name} {nodeTitle}";
                    break;
                }
            }

            return new FindInGraphAdapter.FindSearcherItem(node, title, children: children);
        }
    }
}
