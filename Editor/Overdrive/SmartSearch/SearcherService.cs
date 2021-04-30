using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Searcher;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    class SearcherWindow : EditorWindow
    {
    }

    public interface IGTFSearcherAdapter : ISearcherAdapter
    {
        void SetInitialSplitterDetailRatio(float ratio);
    }

    public static class SearcherService
    {
        public static class Usage
        {
            public const string k_CreateNode = "create-node";
            public const string k_Values = "values";
            public const string k_Types = "types";
        }

        public static SearcherGraphView GraphView { get; }

        static readonly Searcher.SearcherWindow.Alignment k_FindAlignment = new Searcher.SearcherWindow.Alignment(
            Searcher.SearcherWindow.Alignment.Vertical.Top, Searcher.SearcherWindow.Alignment.Horizontal.Center);
        static readonly TypeSearcherAdapter k_TypeAdapter = new TypeSearcherAdapter("Pick a type");
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
            GraphView = new SearcherGraphView(null, null);
        }

        static void ShowSearcher(GraphToolState graphToolState, Vector2 position, Action<GraphNodeModelSearcherItem> callback, List<SearcherDatabaseBase> dbs, SearcherFilter filter, IGTFSearcherAdapter adapter, string usage)
        {
            Rect rect = graphToolState.WindowState.GraphModel.Stencil.GetSearcherRect(position + EditorWindow.focusedWindow.position.position, out float ratio, usage);

            adapter.SetInitialSplitterDetailRatio(ratio);

            EditorWindow window = null;
            var prefs = graphToolState.Preferences;
            if (prefs?.GetBool(BoolPref.SearcherInRegularWindow) ?? false)
                window = PromptSearcherInOwnWindow(dbs, filter, adapter, callback, rect);
            else
                PromptSearcherPopup(dbs, filter, adapter, rect, callback);
            ListenToSearcherSize((Stencil)graphToolState.WindowState.GraphModel.Stencil, usage, window);
        }

        static void ListenToSearcherSize(Stencil stencil, string usage, EditorWindow existingWindow = null)
        {
            // This is a retro engineering of the searcher to get changes in the window size and splitter position
            if (stencil == null)
                return;
            var searcherWindow = existingWindow ?? EditorWindow.GetWindow<Searcher.SearcherWindow>();
            var searcherResizer = searcherWindow.rootVisualElement.Q("windowResizer");
            var rightPanel = searcherWindow.rootVisualElement.Q("windowDetailsVisualContainer");
            var leftPanel = searcherWindow.rootVisualElement.Q("searcherVisualContainer");

            if (searcherResizer != null)
            {
                EventCallback<GeometryChangedEvent> callback = evt =>
                {
                    float ratio = 1.0f;
                    if (rightPanel != null && leftPanel != null)
                        ratio = rightPanel.resolvedStyle.flexGrow / leftPanel.resolvedStyle.flexGrow;

                    stencil.SetSearcherSize(usage ?? "", searcherWindow.position.size, ratio);
                };

                searcherWindow.rootVisualElement.RegisterCallback<GeometryChangedEvent>(callback);
                leftPanel?.RegisterCallback<GeometryChangedEvent>(callback);
            }
        }


        public static void ShowInputToGraphNodes(GraphToolState graphToolState, IEnumerable<IPortModel> portModels, Vector2 position,
            Action<GraphNodeModelSearcherItem> callback)
        {
            var stencil = (Stencil)graphToolState.WindowState.GraphModel.Stencil;
            var filter = stencil.GetSearcherFilterProvider()?.GetInputToGraphSearcherFilter(portModels);
            var adapter = stencil.GetSearcherAdapter(graphToolState.WindowState.GraphModel, "Add an input node", portModels);
            var dbProvider = stencil.GetSearcherDatabaseProvider();

            if (dbProvider == null)
                return;

            var dbs = dbProvider.GetGraphElementsSearcherDatabases(graphToolState.WindowState.GraphModel)
                .Concat(dbProvider.GetGraphVariablesSearcherDatabases(graphToolState.WindowState.GraphModel))
                .Concat(dbProvider.GetDynamicSearcherDatabases(portModels))
                .ToList();

            ShowSearcher(graphToolState, position, callback, dbs, filter, adapter, Usage.k_CreateNode);
        }

        public static void ShowOutputToGraphNodes(GraphToolState graphToolState, IPortModel portModel, Vector2 position,
            Action<GraphNodeModelSearcherItem> callback)
        {
            var stencil = (Stencil)graphToolState.WindowState.GraphModel.Stencil;
            var filter = stencil.GetSearcherFilterProvider()?.GetOutputToGraphSearcherFilter(portModel);
            var adapter = stencil.GetSearcherAdapter(graphToolState.WindowState.GraphModel, $"Choose an action for {portModel.DataTypeHandle.GetMetadata(stencil).FriendlyName}", Enumerable.Repeat(portModel, 1));
            var dbProvider = stencil.GetSearcherDatabaseProvider();

            if (dbProvider == null)
                return;

            var dbs = dbProvider.GetGraphElementsSearcherDatabases(graphToolState.WindowState.GraphModel).ToList();

            ShowSearcher(graphToolState, position, callback, dbs, filter, adapter, Usage.k_CreateNode);
        }

        public static void ShowOutputToGraphNodes(GraphToolState graphToolState, IEnumerable<IPortModel> portModels, Vector2 position,
            Action<GraphNodeModelSearcherItem> callback)
        {
            var stencil = (Stencil)graphToolState.WindowState.GraphModel.Stencil;
            var filter = stencil.GetSearcherFilterProvider()?.GetOutputToGraphSearcherFilter(portModels);
            var adapter = stencil.GetSearcherAdapter(graphToolState.WindowState.GraphModel, $"Choose an action for {portModels.First().DataTypeHandle.GetMetadata(stencil).FriendlyName}", portModels);
            var dbProvider = stencil.GetSearcherDatabaseProvider();

            if (dbProvider == null)
                return;

            var dbs = dbProvider.GetGraphElementsSearcherDatabases(graphToolState.WindowState.GraphModel).ToList();

            ShowSearcher(graphToolState, position, callback, dbs, filter, adapter, Usage.k_CreateNode);
        }

        public static void ShowEdgeNodes(GraphToolState graphToolState, IEdgeModel edgeModel, Vector2 position,
            Action<GraphNodeModelSearcherItem> callback)
        {
            var stencil = (Stencil)graphToolState.WindowState.GraphModel.Stencil;
            var filter = stencil.GetSearcherFilterProvider()?.GetEdgeSearcherFilter(edgeModel);
            var adapter = stencil.GetSearcherAdapter(graphToolState.WindowState.GraphModel, "Insert Node");
            var dbProvider = stencil.GetSearcherDatabaseProvider();

            if (dbProvider == null)
                return;

            var dbs = dbProvider.GetGraphElementsSearcherDatabases(graphToolState.WindowState.GraphModel).ToList();

            ShowSearcher(graphToolState, position, callback, dbs, filter, adapter, Usage.k_CreateNode);
        }

        public static void ShowGraphNodes(GraphToolState graphToolState, Vector2 position, Action<GraphNodeModelSearcherItem> callback)
        {
            var stencil = (Stencil)graphToolState.WindowState.GraphModel.Stencil;
            var filter = stencil.GetSearcherFilterProvider()?.GetGraphSearcherFilter();
            var adapter = stencil.GetSearcherAdapter(graphToolState.WindowState.GraphModel, "Add a graph node");
            var dbProvider = stencil.GetSearcherDatabaseProvider();

            if (dbProvider == null)
                return;

            var dbs = dbProvider.GetGraphElementsSearcherDatabases(graphToolState.WindowState.GraphModel)
                .Concat(dbProvider.GetDynamicSearcherDatabases((IPortModel)null))
                .ToList();

            ShowSearcher(graphToolState, position, callback, dbs, filter, adapter, Usage.k_CreateNode);
        }

        static void PromptSearcherPopup<T>(List<SearcherDatabaseBase> databases, SearcherFilter filter,
            ISearcherAdapter adapter, Rect rect, Action<T> callback) where T : ISearcherItemDataProvider
        {
            ApplyDatabasesFilter<T>(databases, filter);
            var searcher = new Searcher.Searcher(databases, adapter) { SortComparison = k_GraphElementSort };

            Searcher.SearcherWindow.Show(EditorWindow.focusedWindow, searcher, item =>
            {
                if (item is T dataProvider)
                {
                    callback(dataProvider);
                    return true;
                }

                return false;
            }, null, rect);
            ApplyDatabasesFilter<T>(databases, null);


        }

        static EditorWindow PromptSearcherInOwnWindow<T>(List<SearcherDatabaseBase> databases, SearcherFilter filter,
            ISearcherAdapter adapter, Action<T> callback, Rect rect) where T : ISearcherItemDataProvider
        {
            ApplyDatabasesFilter<T>(databases, filter);
            var searcher = new Searcher.Searcher(databases, adapter) { SortComparison = k_GraphElementSort };

            Type searcherControlType = Type.GetType("UnityEditor.Searcher.SearcherControl, Unity.Searcher.Editor");
            bool windowExists = EditorWindow.HasOpenInstances<SearcherWindow>();
            var window = EditorWindow.GetWindow<SearcherWindow>();
            if (!windowExists)
                window.position = rect;

            var searcherControl = Activator.CreateInstance(searcherControlType) as VisualElement;

            Action<SearcherItem> action = item =>
            {
                if (item is T dataProvider)
                {
                    callback(dataProvider);
                }
            };

            var setupMethod = searcherControlType.GetMethod("Setup");
            setupMethod?.Invoke(searcherControl, new object[]
            {
                searcher, action, null
            });

            window.rootVisualElement.Add(searcherControl);

            return window;
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

        public static void FindInGraph(
            EditorWindow host,
            IGraphModel graph,
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

            Searcher.SearcherWindow.Show(host, searcher, item =>
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

            Searcher.SearcherWindow.Show(EditorWindow.focusedWindow, searcher, item =>
            {
                if (item == null)
                    return false;

                callback(((EnumValuesAdapter.EnumValueSearcherItem)item).value, 0);
                return true;
            }, position, null);
        }

        public static void ShowValues(Stencil stencil, string title, IEnumerable<string> values, Vector2 position,
            Action<string, int> callback)
        {
            var rect = stencil.GetSearcherRect(position + EditorWindow.focusedWindow.position.position, out float ratio, Usage.k_Values);

            var items = values.Select(v => new SearcherItem(v)).ToList();
            var database = SearcherDatabase.Create(items, "", false);
            var adapter = new SimpleSearcherAdapter(title);
            adapter.SetInitialSplitterDetailRatio(ratio);
            var searcher = new Searcher.Searcher(database, adapter);

            Searcher.SearcherWindow.Show(EditorWindow.focusedWindow, searcher, item =>
            {
                if (item == null)
                    return false;

                callback(item.Name, item.Id);
                return true;
            }, null, rect);
            ListenToSearcherSize(stencil, Usage.k_Values);
        }

        public static void ShowVariableTypes(Stencil stencil, Vector2 position, Action<TypeHandle, int> callback)
        {
            var databases = stencil.GetSearcherDatabaseProvider()?.GetVariableTypesSearcherDatabases();
            if (databases != null)
                ShowTypes(stencil, databases, position, callback);
        }

        public static void ShowTypes(Stencil stencil, List<SearcherDatabase> databases, Vector2 position, Action<TypeHandle, int> callback)
        {
            var rect = stencil.GetSearcherRect(position + EditorWindow.focusedWindow.position.position, out float ratio, Usage.k_Types);
            k_TypeAdapter.SetInitialSplitterDetailRatio(ratio);

            var searcher = new Searcher.Searcher(databases, k_TypeAdapter) { SortComparison = TypeComparison };

            Searcher.SearcherWindow.Show(EditorWindow.focusedWindow, searcher, item =>
            {
                if (!(item is TypeSearcherItem typeItem))
                    return false;

                callback(typeItem.Type, 0);
                return true;
            }, null, rect);
            ListenToSearcherSize(stencil, Usage.k_Types);
        }

        static SearcherItem MakeFindItems(INodeModel node, string title)
        {
            switch (node)
            {
                // TODO virtual property in NodeModel formatting what's displayed in the find window
                case IConstantNodeModel cnm:
                    {
                        var nodeTitle = cnm.Type == typeof(string) ? $"\"{title}\"" : title;
                        title = $"Const {cnm.Type.Name} {nodeTitle}";
                        break;
                    }
            }

            return new FindInGraphAdapter.FindSearcherItem(node, title);
        }
    }
}
