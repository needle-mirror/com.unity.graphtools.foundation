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

            var view = modelUI.View;
            var context = modelUI.Context;

            var contextualizedGraphElement = m_ContextualizedGraphElements.FirstOrDefault(cge
                => cge.View == view && cge.Context == context);

            if (contextualizedGraphElement == null)
            {
                contextualizedGraphElement = new ContextualizedGraphElements(view, context);
                m_ContextualizedGraphElements.Add(contextualizedGraphElement);
            }

            contextualizedGraphElement.GraphElements[modelUI.Model.Guid] = modelUI;
        }

        public void RemoveGraphElement(IModelUI modelUI)
        {
            if (modelUI.Model == null)
                return;

            var contextualizedGraphElements = m_ContextualizedGraphElements.FirstOrDefault(cge => cge.View == modelUI.View && cge.Context == modelUI.Context);

            contextualizedGraphElements?.GraphElements.Remove(modelUI.Model.Guid);
        }

        public IModelUI FirstOrDefault(IModelView view, string context, IGraphElementModel model)
        {
            if (model == null)
                return null;

            var contextualizedGraphElement = m_ContextualizedGraphElements.FirstOrDefault(gel => gel.View == view && gel.Context == context);

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
