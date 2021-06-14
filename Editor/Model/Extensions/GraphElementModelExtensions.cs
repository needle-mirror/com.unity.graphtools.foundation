using System.Collections.Generic;
using System.Linq;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Extension methods for <see cref="IGraphElementModel"/>.
    /// </summary>
    public static class GraphElementModelExtensions
    {
        /// <summary>
        /// Test if this model has a capability.
        /// </summary>
        /// <param name="self">Element model to test</param>
        /// <param name="capability">Capability to check for</param>
        /// <returns>true if the model has the capability, false otherwise</returns>
        public static bool HasCapability(this IGraphElementModel self, Capabilities capability)
        {
            return self.Capabilities.Any(c => c == capability);
        }

        /// <summary>
        /// Set a capability for a model.
        /// </summary>
        /// <param name="self">Element model to affect</param>
        /// <param name="capability">Capability to set</param>
        /// <param name="active">true to set the capability, false to remove it</param>
        public static void SetCapability(this IGraphElementModel self, Capabilities capability, bool active)
        {
            if (!(self.Capabilities is IList<Capabilities> capabilities))
                return;

            if (active)
            {
                if (!self.HasCapability(capability))
                    capabilities.Add(capability);
            }
            else
            {
                capabilities.Remove(capability);
            }
        }

        /// <summary>
        /// Remove all capabilities from a model.
        /// </summary>
        /// <param name="self">The model to remove capabilites from</param>
        public static void ClearCapabilities(this IGraphElementModel self)
        {
            if (self.Capabilities is List<Capabilities> capabilities)
            {
                capabilities.Clear();
            }
        }

        /// <summary>
        /// Test if a model has the capability to be selected.
        /// </summary>
        /// <param name="self">Model to test</param>
        /// <returns>True if it has the capability, false otherwise.</returns>
        public static bool IsSelectable(this IGraphElementModel self)
        {
            return self.HasCapability(Capabilities.Selectable);
        }

        /// <summary>
        /// Test if a model has the capability to be collapsed.
        /// </summary>
        /// <param name="self">Model to test</param>
        /// <returns>True if it has the capability, false otherwise.</returns>
        public static bool IsCollapsible(this IGraphElementModel self)
        {
            return self.HasCapability(Capabilities.Collapsible);
        }

        /// <summary>
        /// Test if a model has the capability to be resized.
        /// </summary>
        /// <param name="self">Model to test</param>
        /// <returns>True if it has the capability, false otherwise.</returns>
        public static bool IsResizable(this IGraphElementModel self)
        {
            return self.HasCapability(Capabilities.Resizable);
        }

        /// <summary>
        /// Tests if a model has the capability to be moved.
        /// </summary>
        /// <param name="self">Model to test</param>
        /// <returns>True if it has the capability, false otherwise.</returns>
        public static bool IsMovable(this IGraphElementModel self)
        {
            return self.HasCapability(Capabilities.Movable);
        }

        /// <summary>
        /// Tests if a model has the capability to be deleted.
        /// </summary>
        /// <param name="self">Model to test</param>
        /// <returns>True if it has the capability, false otherwise.</returns>
        public static bool IsDeletable(this IGraphElementModel self)
        {
            return self.HasCapability(Capabilities.Deletable);
        }

        /// <summary>
        /// Tests if a model has the capability to be dropped.
        /// </summary>
        /// <param name="self">Model to test</param>
        /// <returns>True if it has the capability, false otherwise.</returns>
        public static bool IsDroppable(this IGraphElementModel self)
        {
            return self.HasCapability(Capabilities.Droppable);
        }

        /// <summary>
        /// Tests if a model has the capability to be renamed.
        /// </summary>
        /// <param name="self">Model to test</param>
        /// <returns>True if it has the capability, false otherwise.</returns>
        public static bool IsRenamable(this IGraphElementModel self)
        {
            return self.HasCapability(Capabilities.Renamable);
        }

        /// <summary>
        /// Tests if a model has the capability to be copied.
        /// </summary>
        /// <param name="self">Model to test</param>
        /// <returns>True if it has the capability, false otherwise.</returns>
        public static bool IsCopiable(this IGraphElementModel self)
        {
            return self.HasCapability(Capabilities.Copiable);
        }

        /// <summary>
        /// Tests if a model has the capability to change color.
        /// </summary>
        /// <param name="self">Model to test</param>
        /// <returns>True if it has the capability, false otherwise.</returns>
        public static bool IsColorable(this IGraphElementModel self)
        {
            return self.HasCapability(Capabilities.Colorable);
        }
    }
}
