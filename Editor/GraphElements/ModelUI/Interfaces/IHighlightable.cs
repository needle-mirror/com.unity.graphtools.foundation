using System;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Interface for graph elements that can be highlighted when another element is selected.
    /// </summary>
    public interface IHighlightable : IGraphElement
    {
        // PF TODO ShouldHighlightItemUsage seems to always be followed by a Highlighted.set
        // PF TODO Highlighted.get is only used in tests.

        /// <summary>
        /// Whether the element is currently highlighted.
        /// </summary>
        bool Highlighted { get; set; }

        /// <summary>
        /// Determines whether this element should be highlighted when otherElement is selected.
        /// </summary>
        /// <param name="selectedElement">The selected element.</param>
        /// <returns>True if this element should be highlighted, false otherwise.</returns>
        bool ShouldHighlightItemUsage(IGraphElementModel selectedElement);
    }
}
