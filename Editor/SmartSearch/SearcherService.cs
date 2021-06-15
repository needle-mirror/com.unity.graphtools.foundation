using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Searcher;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// <see cref="EditorWindow"/> used for the Searcher.
    /// </summary>
    class SearcherWindow : EditorWindow
    {
    }

    /// <summary>
    /// Searcher adapter used in GraphToolsFundations
    /// </summary>
    public interface IGTFSearcherAdapter : ISearcherAdapter
    {
        /// <summary>
        /// Sets the initial ratio for the details panel splitter.
        /// </summary>
        /// <param name="ratio">Ratio to set.</param>
        void SetInitialSplitterDetailRatio(float ratio);
    }

    /// <summary>
    /// Helper class providing searcher related functionality in GraphToolsFundations.
    /// </summary>
    public static class SearcherService
    {
        public static class Usage
        {
            public const string k_CreateNode = "create-node";
            public const string k_Values = "values";
            public const string k_Types = "types";
        }

        public static GraphView GraphView { get; }

        static readonly Searcher.SearcherWindow.Alignment k_FindAlignment = new Searcher.SearcherWindow.Alignment(
            Searcher.SearcherWindow.Alignment.Vertical.Top, Searcher.SearcherWindow.Alignment.Horizontal.Center);
        static readonly TypeSearcherAdapter k_TypeAdapter = new TypeSearcherAdapter();
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
            var searcherSize = graphToolState.GetSearcherSize(usage);
            position += EditorWindow.focusedWindow.position.position;
            var rect = new Rect(position, searcherSize.Size);

            adapter.SetInitialSplitterDetailRatio(searcherSize.RightLeftRatio);

            EditorWindow window = null;
            var prefs = graphToolState.Preferences;
            if (prefs?.GetBool(BoolPref.SearcherInRegularWindow) ?? false)
                window = PromptSearcherInOwnWindow(dbs, filter, adapter, callback, rect);
            else
                PromptSearcherPopup(dbs, filter, adapter, rect, callback);
            ListenToSearcherSize(graphToolState, usage, window);
        }

        static void ListenToSearcherSize(GraphToolState graphToolState, string usage, EditorWindow existingWindow = null)
        {
            // This is a retro engineering of the searcher to get changes in the window size and splitter position
            var searcherWindow = existingWindow != null ? existingWindow : EditorWindow.GetWindow<Searcher.SearcherWindow>();
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

                    // PF FIXME Use command?
                    graphToolState.SetSearcherSize(usage ?? "", searcherWindow.position.size, ratio);
                };

                searcherWindow.rootVisualElement.RegisterCallback(callback);
                leftPanel?.RegisterCallback(callback);
            }
        }


        public static void ShowInputToGraphNodes(Stencil stencil, GraphToolState graphToolState, IEnumerable<IPortModel> portModels, Vector2 position,
            Action<GraphNodeModelSearcherItem> callback)
        {
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

        public static void ShowOutputToGraphNodes(Stencil stencil, GraphToolState graphToolState, IPortModel portModel, Vector2 position,
            Action<GraphNodeModelSearcherItem> callback)
        {
            var filter = stencil.GetSearcherFilterProvider()?.GetOutputToGraphSearcherFilter(portModel);
            var adapter = stencil.GetSearcherAdapter(graphToolState.WindowState.GraphModel, $"Choose an action for {portModel.DataTypeHandle.GetMetadata(stencil).FriendlyName}", Enumerable.Repeat(portModel, 1));
            var dbProvider = stencil.GetSearcherDatabaseProvider();

            if (dbProvider == null)
                return;

            var dbs = dbProvider.GetGraphElementsSearcherDatabases(graphToolState.WindowState.GraphModel).ToList();

            ShowSearcher(graphToolState, position, callback, dbs, filter, adapter, Usage.k_CreateNode);
        }

        public static void ShowOutputToGraphNodes(Stencil stencil, GraphToolState graphToolState, IEnumerable<IPortModel> portModels, Vector2 position,
            Action<GraphNodeModelSearcherItem> callback)
        {
            var filter = stencil.GetSearcherFilterProvider()?.GetOutputToGraphSearcherFilter(portModels);
            var adapter = stencil.GetSearcherAdapter(graphToolState.WindowState.GraphModel, $"Choose an action for {portModels.First().DataTypeHandle.GetMetadata(stencil).FriendlyName}", portModels);
            var dbProvider = stencil.GetSearcherDatabaseProvider();

            if (dbProvider == null)
                return;

            var dbs = dbProvider.GetGraphElementsSearcherDatabases(graphToolState.WindowState.GraphModel).ToList();

            ShowSearcher(graphToolState, position, callback, dbs, filter, adapter, Usage.k_CreateNode);
        }

        public static void ShowEdgeNodes(Stencil stencil, GraphToolState graphToolState, IEdgeModel edgeModel, Vector2 position,
            Action<GraphNodeModelSearcherItem> callback)
        {
            var filter = stencil.GetSearcherFilterProvider()?.GetEdgeSearcherFilter(edgeModel);
            var adapter = stencil.GetSearcherAdapter(graphToolState.WindowState.GraphModel, "Insert Node");
            var dbProvider = stencil.GetSearcherDatabaseProvider();

            if (dbProvider == null)
                return;

            var dbs = dbProvider.GetGraphElementsSearcherDatabases(graphToolState.WindowState.GraphModel).ToList();

            ShowSearcher(graphToolState, position, callback, dbs, filter, adapter, Usage.k_CreateNode);
        }

        public static void ShowGraphNodes(Stencil stencil, GraphToolState graphToolState, Vector2 position, Action<GraphNodeModelSearcherItem> callback)
        {
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
            var searcher = new Searcher.Searcher(databases, adapter, filter) { SortComparison = k_GraphElementSort };

            Searcher.SearcherWindow.Show(EditorWindow.focusedWindow, searcher, item =>
            {
                if (item is T dataProvider)
                {
                    callback(dataProvider);
                    return true;
                }

                return false;
            }, null, rect);
        }

        static EditorWindow PromptSearcherInOwnWindow<T>(List<SearcherDatabaseBase> databases, SearcherFilter filter,
            ISearcherAdapter adapter, Action<T> callback, Rect rect) where T : ISearcherItemDataProvider
        {
            var searcher = new Searcher.Searcher(databases, adapter, filter) { SortComparison = k_GraphElementSort };

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

            var database = new SearcherDatabase(items);
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
            var database = new SearcherDatabase(items);
            var searcher = new Searcher.Searcher(database, new EnumValuesAdapter(title));

            Searcher.SearcherWindow.Show(EditorWindow.focusedWindow, searcher, item =>
            {
                if (item == null)
                    return false;

                callback(((EnumValuesAdapter.EnumValueSearcherItem)item).value, 0);
                return true;
            }, position, null);
        }

        public static void ShowValues(GraphToolState graphToolState, string title, IEnumerable<string> values, Vector2 position,
            Action<string, int> callback)
        {
            var searcherSize = graphToolState.GetSearcherSize(Usage.k_Values);
            position += EditorWindow.focusedWindow.position.position;
            var rect = new Rect(position, searcherSize.Size);

            var items = values.Select(v => new SearcherItem(v)).ToList();
            var database = new SearcherDatabase(items);
            var adapter = new SimpleSearcherAdapter(title);
            adapter.SetInitialSplitterDetailRatio(searcherSize.RightLeftRatio);
            var searcher = new Searcher.Searcher(database, adapter);

            Searcher.SearcherWindow.Show(EditorWindow.focusedWindow, searcher, item =>
            {
                if (item == null)
                    return false;

                callback(item.Name, item.Id);
                return true;
            }, null, rect);
            ListenToSearcherSize(graphToolState, Usage.k_Values);
        }

        public static void ShowVariableTypes(Stencil stencil, GraphToolState graphToolState, Vector2 position, Action<TypeHandle, int> callback)
        {
            var databases = stencil.GetSearcherDatabaseProvider()?.GetVariableTypesSearcherDatabases();
            if (databases != null)
                ShowTypes(graphToolState, databases, position, callback);
        }

        public static void ShowTypes(GraphToolState graphToolState, List<SearcherDatabaseBase> databases, Vector2 position, Action<TypeHandle, int> callback)
        {
            var searcherSize = graphToolState.GetSearcherSize(Usage.k_Types);
            position += EditorWindow.focusedWindow.position.position;
            var rect = new Rect(position, searcherSize.Size);

            k_TypeAdapter.SetInitialSplitterDetailRatio(searcherSize.RightLeftRatio);

            var searcher = new Searcher.Searcher(databases, k_TypeAdapter) { SortComparison = TypeComparison };

            Searcher.SearcherWindow.Show(EditorWindow.focusedWindow, searcher, item =>
            {
                if (!(item is TypeSearcherItem typeItem))
                    return false;

                callback(typeItem.Type, 0);
                return true;
            }, null, rect);
            ListenToSearcherSize(graphToolState, Usage.k_Types);
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
