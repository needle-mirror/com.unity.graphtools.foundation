using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    [Obsolete]
    public static class GraphToolStateExtensions
    {
        [Obsolete("2021-02-19 Use IGraphViewStateComponentUpdater.MarkNew() instead.")]
        public static void MarkNew(this GraphToolState graphToolState, IGraphElementModel model)
        {
            using (var stateUpdater = graphToolState.GraphViewState.Updater)
            {
                stateUpdater.U.MarkNew(model);
            }
        }

        [Obsolete("2021-02-19 Use IGraphViewStateComponentUpdater.MarkChanged() instead.")]
        public static void MarkChanged(this GraphToolState graphToolState, IGraphElementModel model)
        {
            using (var stateUpdater = graphToolState.GraphViewState.Updater)
            {
                stateUpdater.U.MarkChanged(model);
            }
        }

        [Obsolete("2021-02-19 Use IGraphViewStateComponentUpdater.MarkDeleted() instead.")]
        public static void MarkDeleted(this GraphToolState graphToolState, IGraphElementModel model)
        {
            using (var stateUpdater = graphToolState.GraphViewState.Updater)
            {
                stateUpdater.U.MarkDeleted(model);
            }
        }
    }
}
