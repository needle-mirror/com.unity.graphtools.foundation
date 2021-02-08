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

        public void AddOrReplaceUIForModel(IModelUI modelUI)
        {
            if (modelUI.Model == null)
                return;

            var graphView = modelUI.GraphView;
            var context = modelUI.Context;

            var contextualizedGraphElement = m_ContextualizedGraphElements.FirstOrDefault(cge
                => cge.GraphView == graphView && cge.Context == context);

            if (contextualizedGraphElement == null)
            {
                contextualizedGraphElement = new ContextualizedGraphElements(graphView, context);
                m_ContextualizedGraphElements.Add(contextualizedGraphElement);
            }

            contextualizedGraphElement.GraphElements[modelUI.Model.Guid] = modelUI;
        }

        public void RemoveGraphElement(IModelUI modelUI)
        {
            if (modelUI.Model == null)
                return;

            var contextualizedGraphElements = m_ContextualizedGraphElements.FirstOrDefault(cge => cge.GraphView == modelUI.GraphView && cge.Context == modelUI.Context);

            contextualizedGraphElements?.GraphElements.Remove(modelUI.Model.Guid);
        }

        public IModelUI FirstOrDefault(GraphView graphView, string context, IGraphElementModel model)
        {
            if (model == null)
                return null;

            var contextualizedGraphElement = m_ContextualizedGraphElements.FirstOrDefault(gel => gel.GraphView == graphView && gel.Context == context);

            IModelUI modelUI = null;
            contextualizedGraphElement?.GraphElements.TryGetValue(model.Guid, out modelUI);
            return modelUI;
        }

        public IEnumerable<IModelUI> GetAllUIForModel(IGraphElementModel model)
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
