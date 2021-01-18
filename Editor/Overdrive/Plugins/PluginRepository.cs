using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public class PluginRepository : IDisposable
    {
        List<IPluginHandler> m_PluginHandlers;

        internal PluginRepository()
        {
            m_PluginHandlers = new List<IPluginHandler>();
        }

        public IEnumerable<IPluginHandler>  GetPluginHandlers()
        {
            return m_PluginHandlers;
        }

        public void RegisterPlugins(IEnumerable<IPluginHandler> plugins, Store store, GraphViewEditorWindow window)
        {
            UnregisterPlugins(plugins);
            foreach (IPluginHandler handler in plugins)
            {
                handler.Register(store, window);
                m_PluginHandlers.Add(handler);
            }
        }

        public void UnregisterPlugins(IEnumerable<IPluginHandler> except = null)
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
