using System.Collections.Generic;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    class ContextualizedGraphElements
    {
        public readonly IModelView View;
        public readonly string Context;
        public readonly Dictionary<SerializableGUID, IModelUI> GraphElements;

        public ContextualizedGraphElements(IModelView view, string context)
        {
            View = view;
            Context = context;
            GraphElements = new Dictionary<SerializableGUID, IModelUI>();
        }
    }
}
