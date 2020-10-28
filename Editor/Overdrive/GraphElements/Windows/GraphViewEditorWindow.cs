using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor.GraphToolsFoundation.Overdrive.Bridge;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public abstract class GraphViewEditorWindow : GraphViewEditorWindowBridge
    {
        Store m_Store;
        protected GraphView m_GraphView;
        protected VisualElement m_GraphContainer;
        protected BlankPage m_BlankPage;
        protected VisualElement m_SidePanel;
        protected Label m_SidePanelTitle;
        protected Label m_CompilationPendingLabel;
        protected MainToolbar m_MainToolbar;
        protected string m_LastGraphFilePath;

        public virtual IEnumerable<GraphView> GraphViews { get; }
        public Store Store
        {
            get => m_Store;
            private set => m_Store = value;
        }

        public GraphView GraphView => m_GraphView;
        protected MainToolbar MainToolbar => m_MainToolbar;

        protected abstract State CreateInitialState();

        protected virtual void RegisterReducers()
        {
            StoreHelper.RegisterDefaultReducers(Store);
        }

        protected virtual void OnEnable()
        {
            var initialState = CreateInitialState();
            Store = new Store(initialState);
            RegisterReducers();
        }

        protected virtual void OnDisable()
        {
            Store.Dispose();
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
            var graphModel = m_Store.GetState().CurrentGraphModel;

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
            m_Store.GetState().UnloadCurrentGraphAsset();
            m_LastGraphFilePath = null;
            UpdateGraphContainer();
        }

        public void UnloadGraphIfDeleted()
        {
            var iGraphModel = m_Store.GetState().AssetModel as ScriptableObject;
            if (!iGraphModel)
            {
                UnloadGraph();
            }
        }
    }
}
