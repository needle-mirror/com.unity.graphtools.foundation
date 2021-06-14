using System.Collections.Generic;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Interface for placemats.
    /// </summary>
    public interface IPlacematModel : IHasTitle, IMovable, ICollapsible, IResizable, IRenamable, IDestroyable
    {
        /// <summary>
        /// Z-order of the placemat.
        /// </summary>
        /// <remarks>
        /// Higher number is stacked on top.
        /// </remarks>
        int ZOrder { get; set; }

        /// <summary>
        /// Elements hidden in the placemat.
        /// </summary>
        IEnumerable<IGraphElementModel> HiddenElements { get; set; }
    }
}
