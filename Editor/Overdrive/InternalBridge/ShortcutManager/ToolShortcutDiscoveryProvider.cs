using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor.ShortcutManagement;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Bridge
{
    /// <summary>
    /// A shortcut discovery provider for shortcuts defined by Graph Tools Foundation.
    /// We need this special provider because the shortcut defined by GTF must be
    /// associated to a tool, not to GTF itself. We delegate the shortcut discovery
    /// to a proxy that will be set in GTF code.
    /// </summary>
    public sealed class ToolShortcutDiscoveryProvider : IDiscoveryShortcutProvider
    {
        /// <summary>
        /// The single instance of this class.
        /// </summary>
        public static ToolShortcutDiscoveryProvider Instance { get; } = new ToolShortcutDiscoveryProvider();

        /// <summary>
        /// The proxy to which we delegate the shortcut discovery.
        /// </summary>
        public IDiscoveryShortcutProviderProxy Proxy { get; set; }

        ToolShortcutDiscoveryProvider() { }

        /// <summary>
        /// Forces rebuilding the shortcut.
        /// </summary>
        public static void RebuildShortcuts()
        {
            ShortcutIntegration.instance.RebuildShortcuts();
        }

        IEnumerable<IShortcutEntryDiscoveryInfo> IDiscoveryShortcutProvider.GetDefinedShortcuts()
        {
            return Proxy?.GetDefinedShortcuts().Select(si => new ToolShortcutEntryInfo(si)) ?? Enumerable.Empty<IShortcutEntryDiscoveryInfo>();
        }

        // Add ourself to the shortcut provider list.
        [InitializeOnLoadMethod]
        static void InitializeShortcuts()
        {
            var controller = ShortcutIntegration.instance;

            // Get controller.m_Discovery and cast it to UnityEditor.ShortcutManagement.Discovery
            var discoveryField = controller.GetType().GetField("m_Discovery", BindingFlags.NonPublic | BindingFlags.Instance);

            if (!(discoveryField?.GetValue(controller) is Discovery discovery))
                return;

            // Get discovery.m_ShortcutProviders and replace it by our own (current + ours)
            var shortcutProvidersField = discovery.GetType().GetField("m_ShortcutProviders", BindingFlags.NonPublic | BindingFlags.Instance);

            if (!(shortcutProvidersField?.GetValue(discovery) is IEnumerable<IDiscoveryShortcutProvider> shortcutProviders))
                return;

            var newValue = shortcutProviders.ToList();
            newValue.Add(Instance);
            shortcutProvidersField.SetValue(discovery, newValue.ToArray());

            controller.RebuildShortcuts();
        }
    }
}
