using System.Collections.Generic;
using System.Linq;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public static class GraphElementModelExtensions
    {
        public static bool HasCapability(this IGraphElementModel self, Capabilities capability)
        {
            return self.Capabilities.Any(c => c == capability);
        }

        public static void SetCapability(this IGraphElementModel self, Capabilities capability, bool active)
        {
            // Special value we don't allow to set.
            if (capability == Capabilities.NoCapabilities)
                return;

            if (self.Capabilities is List<Capabilities> capabilities)
            {
                if (active)
                {
                    capabilities.Remove(Capabilities.NoCapabilities);
                    if (!self.HasCapability(capability))
                        capabilities.Add(capability);
                }
                else
                {
                    capabilities.Remove(capability);

                    // Due to a quirk of serialization that doesn't let us distinguish between an uninitialized list
                    // of capabilities (when loading older graphs) and an empty list of capabilities, we never let a
                    // list of capabilities empty
                    if (!capabilities.Any())
                        capabilities.Add(Capabilities.NoCapabilities);
                }
            }
        }

        public static void ClearCapabilities(this IGraphElementModel self)
        {
            if (self.Capabilities is List<Capabilities> capabilities)
            {
                capabilities.Clear();
                capabilities.Add(Capabilities.NoCapabilities);
            }
        }

        public static bool IsSelectable(this IGraphElementModel self)
        {
            return self.HasCapability(Capabilities.Selectable);
        }

        public static bool IsCollapsible(this IGraphElementModel self)
        {
            return self.HasCapability(Capabilities.Collapsible);
        }

        public static bool IsResizable(this IGraphElementModel self)
        {
            return self.HasCapability(Capabilities.Resizable);
        }

        public static bool IsMovable(this IGraphElementModel self)
        {
            return self.HasCapability(Capabilities.Movable);
        }

        public static bool IsDeletable(this IGraphElementModel self)
        {
            return self.HasCapability(Capabilities.Deletable);
        }

        public static bool IsDroppable(this IGraphElementModel self)
        {
            return self.HasCapability(Capabilities.Droppable);
        }

        public static bool IsRenamable(this IGraphElementModel self)
        {
            return self.HasCapability(Capabilities.Renamable);
        }

        public static bool IsCopiable(this IGraphElementModel self)
        {
            return self.HasCapability(Capabilities.Copiable);
        }
    }
}
