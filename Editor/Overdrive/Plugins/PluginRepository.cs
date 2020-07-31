using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive.GraphElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    class PluginRepository : IPluginRepository, IDisposable
    {
        Store m_Store;
        GraphViewEditorWindow m_GraphView;
        List<IPluginHandler> m_PluginHandlers;

        internal PluginRepository(Store store, GraphViewEditorWindow graphView)
        {
            m_Store = store;
            m_GraphView = graphView;
            m_PluginHandlers = new List<IPluginHandler>();
        }

        public IEnumerable<IPluginHandler>  GetPluginHandlers()
        {
            return m_PluginHandlers;
        }

        public void RegisterPlugins(IEnumerable<IPluginHandler> plugins)
        {
            UnregisterPlugins(plugins);
            foreach (IPluginHandler handler in plugins)
            {
                handler.Register(m_Store, m_GraphView);
                m_PluginHandlers.Add(handler);
            }
        }

        public void UnregisterPlugins(IEnumerable<IPluginHandler> except)
        {
            foreach (var plugin in m_PluginHandlers)
            {
                if (except == null || !except.Contains(plugin))
                    plugin.Unregister();
            }
            m_PluginHandlers.Clear();
        }

        public IEnumerable<IPluginHandler> RegisteredPlugins => m_PluginHandlers;

        public void Dispose()
        {
            UnregisterPlugins(null);
        }
    }
}
