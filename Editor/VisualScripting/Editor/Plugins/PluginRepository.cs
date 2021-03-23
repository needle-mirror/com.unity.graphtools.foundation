using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEditor.VisualScripting.Model;
using UnityEngine.VisualScripting;

namespace UnityEditor.VisualScripting.Editor.Plugins
{
    public interface IPluginRepository
    {
        void RegisterPlugins(CompilationOptions getCompilationOptions);
        void UnregisterPlugins();
        IEnumerable<IPluginHandler> RegisteredPlugins { get; }
    }

    class PluginRepository : IPluginRepository, IDisposable
    {
        Store m_Store;
        GraphView m_GraphView;
        List<IPluginHandler> m_PluginHandlers;

        internal PluginRepository(Store store, GraphView graphView)
        {
            m_Store = store;
            m_GraphView = graphView;
            m_PluginHandlers = new List<IPluginHandler>();
        }

        public IEnumerable<IPluginHandler> GetPluginHandlers()
        {
            return m_PluginHandlers;
        }

        public void RegisterPlugins(CompilationOptions getCompilationOptions)
        {
            var currentGraphModel = (VSGraphModel)m_Store.GetState().CurrentGraphModel;
            if (currentGraphModel == null) return;
            UnregisterPlugins();
            foreach (IPluginHandler handler in currentGraphModel.Stencil.GetCompilationPluginHandlers(getCompilationOptions))
            {
                handler.Register(m_Store, m_GraphView);
                m_PluginHandlers.Add(handler);
            }
        }

        public void UnregisterPlugins()
        {
            foreach (var plugin in m_PluginHandlers)
            {
                plugin.Unregister();
            }
            m_PluginHandlers.Clear();
        }

        public IEnumerable<IPluginHandler> RegisteredPlugins => m_PluginHandlers;

        public void Dispose()
        {
            UnregisterPlugins();
        }
    }
}
