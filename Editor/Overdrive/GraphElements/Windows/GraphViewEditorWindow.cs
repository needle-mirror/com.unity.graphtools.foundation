using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor.GraphToolsFoundation.Overdrive.Bridge;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public abstract class GraphViewEditorWindow : EditorWindow
    {
        [SerializeField]
        GUID m_GUID;

        protected GraphView m_GraphView;
        protected VisualElement m_GraphContainer;
        protected BlankPage m_BlankPage;
        protected VisualElement m_SidePanel;
        protected Label m_SidePanelTitle;
        protected Label m_CompilationPendingLabel;
        protected MainToolbar m_MainToolbar;
        protected ErrorToolbar m_ErrorToolbar;

        public GUID GUID => m_GUID;

        public virtual IEnumerable<GraphView> GraphViews { get; }

        public Store Store { get; private set; }

        public GraphView GraphView => m_GraphView;
        protected MainToolbar MainToolbar => m_MainToolbar;

        protected abstract State CreateInitialState();

        protected virtual void RegisterReducers()
        {
            StoreHelper.RegisterDefaultReducers(Store);
        }

        protected virtual BlankPage CreateBlankPage()
        {
            return new BlankPage(Store);
        }

        protected abstract MainToolbar CreateMainToolbar();

        protected abstract ErrorToolbar CreateErrorToolbar();

        protected abstract GtfoGraphView CreateGraphView();

        protected virtual void Reset()
        {
            if (Store?.State == null)
                return;

            Store.State.WindowState.CurrentGraph = new OpenedGraph(null, null);
        }

        protected virtual void OnEnable()
        {
            // When we open a window (including when we start the Editor), a new GUID is assigned.
            // When a window is opened and there is a domain reload, the GUID stays the same.
            if (m_GUID == default)
            {
                m_GUID = GUID.Generate();
            }

            var initialState = CreateInitialState();
            Store = new Store(initialState);
            RegisterReducers();

            rootVisualElement.Clear();
            rootVisualElement.pickingMode = PickingMode.Ignore;

            m_GraphContainer = new VisualElement { name = "graphContainer" };
            m_GraphView = CreateGraphView();
            m_MainToolbar = CreateMainToolbar();
            m_ErrorToolbar = CreateErrorToolbar();
            m_BlankPage = CreateBlankPage();
            m_BlankPage?.CreateUI();

            if (m_MainToolbar != null)
                rootVisualElement.Add(m_MainToolbar);
            // AddTracingTimeline();
            rootVisualElement.Add(m_GraphContainer);
            if (m_ErrorToolbar != null)
                m_GraphView.Add(m_ErrorToolbar);

            m_GraphContainer.Add(m_GraphView);

            rootVisualElement.name = "gtfRoot";
            rootVisualElement.AddStylesheet("GraphViewWindow.uss");

            // PF FIXME: Use EditorApplication.playModeStateChanged / AssemblyReloadEvents ? Make sure it works on all domain reloads.

            // After a domain reload, all loaded objects will get reloaded and their OnEnable() called again
            // It looks like all loaded objects are put in a deserialization/OnEnable() queue
            // the previous graph's nodes/edges/... might be queued AFTER this window's OnEnable
            // so relying on objects to be loaded/initialized is not safe
            // hence, we need to defer the loading action
            rootVisualElement.schedule.Execute(() =>
            {
                var lastGraphFilePath = Store.State.WindowState.LastOpenedGraph.GraphAssetModelPath;
                if (!string.IsNullOrEmpty(lastGraphFilePath))
                {
                    try
                    {
                        Store.Dispatch(new LoadGraphAssetAction(
                            lastGraphFilePath,
                            Store.State.WindowState.LastOpenedGraph.BoundObject,
                            LoadGraphAssetAction.Type.KeepHistory));
                    }
                    catch (Exception e)
                    {
                        Debug.LogError(e);
                    }
                }
                else
                {
                    // Force display of blank page.
                    Store.MarkStateDirty();
                }
            }).ExecuteLater(0);
        }

        protected virtual void OnDisable()
        {
            UnloadGraph();

            if (m_ErrorToolbar != null)
                m_GraphView.Remove(m_ErrorToolbar);
            rootVisualElement.Remove(m_GraphContainer);
            if (m_MainToolbar != null)
                rootVisualElement.Remove(m_MainToolbar);

            m_GraphView = null;
            m_MainToolbar = null;
            m_ErrorToolbar = null;
            m_BlankPage = null;

            // Calling Dispose() manually to clean things up now, not at GC time.
            Store.Dispose();
            Store = null;
        }

        protected virtual void OnDestroy()
        {
            // When window is closed, remove all associated state to avoid cluttering the Library folder.
            PersistedEditorState.RemoveViewState(GUID);
        }

        public override IEnumerable<Type> GetExtraPaneTypes()
        {
            return Assembly
                .GetAssembly(typeof(GraphViewToolWindow))
                .GetTypes()
                .Where(myType => myType.IsClass && !myType.IsAbstract && myType.IsSubclassOf(typeof(GraphViewToolWindow)));
        }

        public static void ShowGraphViewWindowWithTools<T>() where T : GraphViewEditorWindow
        {
            var windows = GraphViewStaticBridge.ShowGraphViewWindowWithTools(typeof(GraphViewBlackboardWindow), typeof(GraphViewMinimapWindow), typeof(T));
            var graphView = (windows[0] as T)?.GraphViews.FirstOrDefault();
            if (graphView != null)
            {
                (windows[1] as GraphViewBlackboardWindow)?.SelectGraphViewFromWindow((windows[0] as T), graphView);
                (windows[2] as GraphViewMinimapWindow)?.SelectGraphViewFromWindow((windows[0] as T), graphView);
            }
        }

        protected void UpdateGraphContainer()
        {
            var graphModel = Store?.State?.GraphModel;

            if (graphModel != null)
            {
                if (m_GraphContainer.Contains(m_BlankPage))
                    m_GraphContainer.Remove(m_BlankPage);
                if (!m_GraphContainer.Contains(m_GraphView))
                    m_GraphContainer.Insert(0, m_GraphView);
                if (!m_GraphContainer.Contains(m_SidePanel))
                    m_GraphContainer.Add(m_SidePanel);
                if (!rootVisualElement.Contains(m_CompilationPendingLabel))
                    rootVisualElement.Add(m_CompilationPendingLabel);
            }
            else
            {
                if (m_GraphContainer.Contains(m_SidePanel))
                    m_GraphContainer.Remove(m_SidePanel);
                if (m_GraphContainer.Contains(m_GraphView))
                    m_GraphContainer.Remove(m_GraphView);
                if (!m_GraphContainer.Contains(m_BlankPage))
                    m_GraphContainer.Insert(0, m_BlankPage);
                if (rootVisualElement.Contains(m_CompilationPendingLabel))
                    rootVisualElement.Remove(m_CompilationPendingLabel);
            }
        }

        public virtual void UnloadGraph()
        {
            Store.State.UnloadCurrentGraphAsset();
            UpdateGraphContainer();
        }

        public void UnloadGraphIfDeleted()
        {
            var iGraphModel = Store.State.AssetModel as ScriptableObject;
            if (!iGraphModel)
            {
                UnloadGraph();
            }
        }
    }
}
