using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public static class StateExtensions
    {
        public static void MarkNew(this State state, IGraphElementModel model)
        {
            state.MarkNew(new[] { model });
        }

        public static void MarkChanged(this State state, IGraphElementModel model)
        {
            state.MarkChanged(new[] { model });
        }

        public static void MarkDeleted(this State state, IGraphElementModel model)
        {
            state.MarkDeleted(new[] { model });
        }
    }
}
