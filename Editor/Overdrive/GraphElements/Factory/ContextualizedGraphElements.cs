using System;
using System.Collections.Generic;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    class ContextualizedGraphElements
    {
        public readonly GraphView GraphView;
        public readonly string Context;
        public readonly Dictionary<GUID, IGraphElement> GraphElements;

        public ContextualizedGraphElements(GraphView graphView, string context)
        {
            GraphView = graphView;
            Context = context;
            GraphElements = new Dictionary<GUID, IGraphElement>();
        }
    }
}
