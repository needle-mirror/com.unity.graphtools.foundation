using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.GraphToolsFoundation.Overdrive.VisualScripting;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.Plugins
{
    public interface IPluginRepository
    {
        void RegisterPlugins(CompilationOptions getCompilationOptions);
        void UnregisterPlugins(IEnumerable<IPluginHandler> except = null);
        IEnumerable<IPluginHandler> RegisteredPlugins { get; }
    }

    class PluginRepository : IPluginRepository, IDisposable
    {
        Store m_Store;
        VseWindow m_GraphView;
        List<IPluginHandler> m_PluginHandlers;

        internal PluginRepository(Store store, VseWindow graphView)
        {
            m_Store = store;
            m_GraphView = graphView;
            m_PluginHandlers = new List<IPluginHandler>();
        }

        public IEnumerable<IPluginHandler>  GetPluginHandlers()
        {
            return m_PluginHandlers;
        }

        public void RegisterPlugins(CompilationOptions getCompilationOptions)
        {
            var currentGraphModel = (VSGraphModel)m_Store.GetState().CurrentGraphModel;
            if (currentGraphModel == null) return;
            var compilationPluginHandlers = currentGraphModel.Stencil?.GetCompilationPluginHandlers(getCompilationOptions) ?? Enumerable.Empty<IPluginHandler>();
            UnregisterPlugins(compilationPluginHandlers);
            foreach (IPluginHandler handler in compilationPluginHandlers)
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
