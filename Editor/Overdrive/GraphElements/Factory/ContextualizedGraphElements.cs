using System.Collections.Generic;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    class ContextualizedGraphElements
    {
        public readonly GraphView GraphView;
        public readonly string Context;
        public readonly Dictionary<SerializableGUID, IModelUI> GraphElements;

        public ContextualizedGraphElements(GraphView graphView, string context)
        {
            GraphView = graphView;
            Context = context;
            GraphElements = new Dictionary<SerializableGUID, IModelUI>();
        }
    }
}
