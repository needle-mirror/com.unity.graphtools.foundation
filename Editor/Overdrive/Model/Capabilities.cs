using System;
using System.Collections.Generic;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    // ReSharper disable InconsistentNaming
    [InitializeOnLoad]
    public class Capabilities : Enumeration
    {
        const string k_CapabilityPrefix = "";
        const string k_OldCapabilityPrefix = "GraphToolsFoundation";

        static readonly Dictionary<int, Capabilities> s_Capabilities = new Dictionary<int, Capabilities>();
        static readonly Dictionary<int, Capabilities> s_CapabilitiesByName = new Dictionary<int, Capabilities>();

        static int s_NextId;

        public static readonly Capabilities NoCapabilities;

        public static readonly Capabilities Selectable;
        public static readonly Capabilities Deletable;
        public static readonly Capabilities Droppable;
        public static readonly Capabilities Copiable;
        public static readonly Capabilities Renamable;
        public static readonly Capabilities Movable;
        public static readonly Capabilities Resizable;
        public static readonly Capabilities Collapsible;

        static Capabilities()
        {
            s_NextId = 0;

            // Special value present when there's no capabilities. Required due to a quirk of serialization that doesn't
            // let us distinguish between an uninitialized list of capabilities (when loading older graphs) and an empty
            // list of capabilities.
            NoCapabilities = new Capabilities(Int32.MinValue, nameof(NoCapabilities));

            Selectable = new Capabilities(nameof(Selectable));
            Deletable = new Capabilities(nameof(Deletable));
            Droppable = new Capabilities(nameof(Droppable));
            Copiable = new Capabilities(nameof(Copiable));
            Renamable = new Capabilities(nameof(Renamable));
            Movable = new Capabilities(nameof(Movable));
            Resizable = new Capabilities(nameof(Resizable));
            Collapsible = new Capabilities(nameof(Collapsible));
        }

        protected Capabilities(string name, string prefix = k_CapabilityPrefix)
            : this(s_NextId++, prefix + "." + name)
        {}

        Capabilities(int id, string name) : base(id, name)
        {
            if (s_Capabilities.ContainsKey(id))
                throw new ArgumentException($"Id {id} used for Capability {Name} is already used for Capability {s_Capabilities[id].Name}");
            s_Capabilities[id] = this;

            int hash = Name.GetHashCode();
            if (s_CapabilitiesByName.ContainsKey(hash))
                throw new ArgumentException($"Name {Name} is already used for Capability.");
            s_CapabilitiesByName[hash] = this;
        }

        public static Capabilities Get(int id) => s_Capabilities[id];

        public static Capabilities Get(string fullname)
        {
            // TODO JOCE Remove this check before we go to 1.0
            if (fullname.StartsWith(k_OldCapabilityPrefix))
                fullname = fullname.Substring(k_OldCapabilityPrefix.Length);
            return s_CapabilitiesByName[fullname.GetHashCode()];
        }
    }
}
