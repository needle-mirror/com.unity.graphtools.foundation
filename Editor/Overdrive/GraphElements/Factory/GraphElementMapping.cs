using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    class GraphElementMapping
    {
        List<ContextualizedGraphElements> m_ContextualizedGraphElements;

        public GraphElementMapping()
        {
            m_ContextualizedGraphElements = new List<ContextualizedGraphElements>();
        }

        public void AddOrReplaceUIForModel(IGraphElement graphElement)
        {
            if (graphElement.Model == null)
                return;

            var graphView = graphElement.GraphView;
            var context = graphElement.Context;

            var contextualizedGraphElement = m_ContextualizedGraphElements.FirstOrDefault(cge
                => cge.GraphView == graphView && cge.Context == context);

            if (contextualizedGraphElement == null)
            {
                contextualizedGraphElement = new ContextualizedGraphElements(graphView, context);
                m_ContextualizedGraphElements.Add(contextualizedGraphElement);
            }

            contextualizedGraphElement.GraphElements[graphElement.Model.Guid] = graphElement;
        }

        public void RemoveGraphElement(IGraphElement graphElement)
        {
            if (graphElement.Model == null)
                return;

            var contextualizedGraphElements = m_ContextualizedGraphElements.FirstOrDefault(cge => cge.GraphView == graphElement.GraphView && cge.Context == graphElement.Context);

            contextualizedGraphElements?.GraphElements.Remove(graphElement.Model.Guid);
        }

        public IGraphElement FirstOrDefault(GraphView graphView, string context, IGraphElementModel model)
        {
            if (model == null)
                return null;

            var contextualizedGraphElement = m_ContextualizedGraphElements.FirstOrDefault(gel => gel.GraphView == graphView && gel.Context == context);

            IGraphElement graphElement = null;
            contextualizedGraphElement?.GraphElements.TryGetValue(model.Guid, out graphElement);
            return graphElement;
        }

        public IEnumerable<IGraphElement> GetAllUIForModel(IGraphElementModel model)
        {
            if (model == null)
                yield break;

            foreach (var contextualizedGraphElement in m_ContextualizedGraphElements)
            {
                if (contextualizedGraphElement.GraphElements.TryGetValue(model.Guid, out var graphElement))
                {
                    yield return graphElement;
                }
            }
        }
    }
}
