using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    [Obsolete("2021-01-05 StateExtensions was renamed to GraphToolStateExtensions (UnityUpgradable) -> GraphToolStateExtensions")]
    public static class StateExtensions {}

    public static class GraphToolStateExtensions
    {
        public static void MarkNew(this GraphToolState graphToolState, IGraphElementModel model)
        {
            graphToolState.MarkNew(new[] { model });
        }

        public static void MarkChanged(this GraphToolState graphToolState, IGraphElementModel model)
        {
            graphToolState.MarkChanged(new[] { model });
        }

        public static void MarkDeleted(this GraphToolState graphToolState, IGraphElementModel model)
        {
            graphToolState.MarkDeleted(new[] { model });
        }
    }
}
