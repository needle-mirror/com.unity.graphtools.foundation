using System;
using System.Collections.Generic;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public interface IPluginRepository
    {
        void RegisterPlugins(IEnumerable<IPluginHandler> plugins);
        void UnregisterPlugins(IEnumerable<IPluginHandler> except = null);
        IEnumerable<IPluginHandler> RegisteredPlugins { get; }
    }
}
