using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    [Serializable]
    public class SelectionStateComponent : AssetViewStateComponent
    {
        [SerializeField]
        List<string> m_ElementModelsToSelectUponCreation;

        public SelectionStateComponent()
        {
            m_ElementModelsToSelectUponCreation = new List<string>();
        }

        public virtual bool ShouldSelectElementUponCreation(IGraphElementModel model)
        {
            return m_ElementModelsToSelectUponCreation.Contains(model.Guid.ToString());
        }

        public virtual void SelectElementsUponCreation(IEnumerable<IGraphElementModel> graphElementModels, bool select)
        {
            if (select)
            {
                m_ElementModelsToSelectUponCreation.AddRange(graphElementModels.Select(x => x.Guid.ToString()));
            }
            else
            {
                foreach (var graphElementModel in graphElementModels)
                    m_ElementModelsToSelectUponCreation.Remove(graphElementModel.Guid.ToString());
            }
        }

        public virtual void ClearElementsToSelectUponCreation()
        {
            m_ElementModelsToSelectUponCreation.Clear();
        }
    }
}
