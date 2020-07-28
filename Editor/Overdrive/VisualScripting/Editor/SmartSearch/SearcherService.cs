using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using UnityEditor.Searcher;
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
            var store = new Store(new State(new VSEditorDataModel(null)), StoreHelper.RegisterReducers);
            GraphView = new SearcherGraphView(store);
        }

        public static void ShowInputToGraphNodes(Overdrive.State state, IGTFPortModel portModel, Vector2 position,
            Action<GraphNodeModelSearcherItem> callback)
        {
            var stencil = state.CurrentGraphModel.Stencil;
            var filter = stencil.GetSearcherFilterProvider()?.GetInputToGraphSearcherFilter(portModel);
            var adapter = stencil.GetSearcherAdapter(state.CurrentGraphModel, "Add an input node", portModel);
            var dbProvider = stencil.GetSearcherDatabaseProvider();
            var dbs = dbProvider.GetGraphElementsSearcherDatabases(state.CurrentGraphModel)
                .Concat(dbProvider.GetGraphVariablesSearcherDatabases(state.CurrentGraphModel))
                .Concat(dbProvider.GetDynamicSearcherDatabases(portModel))
                .ToList();

            PromptSearcher(dbs, filter, adapter, position, callback);
        }

        public static void ShowOutputToGraphNodes(Overdrive.State state, IGTFPortModel portModel, Vector2 position,
            Action<GraphNodeModelSearcherItem> callback)
        {
            var stencil = state.CurrentGraphModel.Stencil;
            var filter = stencil.GetSearcherFilterProvider()?.GetOutputToGraphSearcherFilter(portModel);
            var adapter = stencil.GetSearcherAdapter(state.CurrentGraphModel, $"Choose an action for {portModel.DataTypeHandle.GetMetadata(stencil).FriendlyName}", portModel);
            var dbProvider = stencil.GetSearcherDatabaseProvider();
            var dbs = dbProvider.GetGraphElementsSearcherDatabases(state.CurrentGraphModel).ToList();

            PromptSearcher(dbs, filter, adapter, position, callback);
        }

        public static void ShowEdgeNodes(Overdrive.State state, IGTFEdgeModel edgeModel, Vector2 position,
            Action<GraphNodeModelSearcherItem> callback)
        {
            var stencil = state.CurrentGraphModel.Stencil;
            var filter = stencil.GetSearcherFilterProvider()?.GetEdgeSearcherFilter(edgeModel);
            var adapter = stencil.GetSearcherAdapter(state.CurrentGraphModel, "Insert Node");
            var dbProvider = stencil.GetSearcherDatabaseProvider();
            var dbs = dbProvider.GetGraphElementsSearcherDatabases(state.CurrentGraphModel).ToList();

            PromptSearcher(dbs, filter, adapter, position, callback);
        }

        public static void ShowGraphNodes(Overdrive.State state, Vector2 position, Action<GraphNodeModelSearcherItem> callback)
        {
            var stencil = state.CurrentGraphModel.Stencil;
            var filter = stencil.GetSearcherFilterProvider()?.GetGraphSearcherFilter();
            var adapter = stencil.GetSearcherAdapter(state.CurrentGraphModel, "Add a graph node");
            var dbProvider = stencil.GetSearcherDatabaseProvider();
            var dbs = dbProvider.GetGraphElementsSearcherDatabases(state.CurrentGraphModel)
                .Concat(dbProvider.GetDynamicSearcherDatabases(null))
                .ToList();

            PromptSearcher(dbs, filter, adapter, position, callback);
        }

        static void PromptSearcher<T>(List<SearcherDatabaseBase> databases, SearcherFilter filter,
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
            ApplyDatabasesFilter<T>(databases, null);
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

        public static void ApplyDatabasesFilter<T>(List<SearcherDatabaseBase> databases, SearcherFilter filter)
            where T : ISearcherItemDataProvider
        {
            foreach (var database in databases)
            {
                if (database is LuceneSearcherDatabase luceneSearcherDatabase)
                {
                    luceneSearcherDatabase.SetFilters(filter?.LuceneFilters);
                }
            }
        }

        internal static void FindInGraph(
            EditorWindow host,
            IGTFGraphModel graph,
            Action<FindInGraphAdapter.FindSearcherItem> highlightDelegate,
            Action<FindInGraphAdapter.FindSearcherItem> selectionDelegate
        )
        {
            var items = graph.NodeModels
                .Where(x => x is IHasTitle titled && !string.IsNullOrEmpty(titled.Title))
                .Select(x => MakeFindItems(x, (x as IHasTitle)?.Title))
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

        public static void ShowVariableTypes(Stencil stencil, Vector2 position, Action<TypeHandle, int> callback)
        {
            var databases = stencil.GetSearcherDatabaseProvider().GetVariableTypesSearcherDatabases();
            ShowTypes(databases, position, callback);
        }

        public static void ShowTypes(List<SearcherDatabase> databases, Vector2 position, Action<TypeHandle, int> callback)
        {
            var searcher = new Searcher.Searcher(databases, k_TypeAdapter) {SortComparison = TypeComparison};
            SearcherWindow.Show(EditorWindow.focusedWindow, searcher, item =>
            {
                if (!(item is TypeSearcherItem typeItem))
                    return false;

                callback(typeItem.Type, 0);
                return true;
            }, position, null);
        }

        static SearcherItem MakeFindItems(IGTFNodeModel node, string title)
        {
            switch (node)
            {
                // TODO virtual property in NodeModel formatting what's displayed in the find window
                case ConstantNodeModel cnm:
                {
                    var nodeTitle = cnm.Value is StringConstant ? $"\"{title}\"" : title;
                    title = $"Const {cnm.Type.Name} {nodeTitle}";
                    break;
                }
            }

            return new FindInGraphAdapter.FindSearcherItem(node, title);
        }
    }
}
