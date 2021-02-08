using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public class PluginRepository : IDisposable
    {
        List<IPluginHandler> m_PluginHandlers;
        GraphViewEditorWindow m_Window;

        internal PluginRepository(GraphViewEditorWindow window)
        {
            m_PluginHandlers = new List<IPluginHandler>();
            m_Window = window;
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
                handler.Register(m_Window);
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
