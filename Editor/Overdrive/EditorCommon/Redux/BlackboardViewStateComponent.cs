using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    [Serializable]
    public class BlackboardViewStateComponent : EditorStateComponent
    {
        [SerializeField]
        List<string> m_BlackboardExpandedRowStates;

        public BlackboardViewStateComponent()
        {
            m_BlackboardExpandedRowStates = new List<string>();
        }

        public void SetVariableDeclarationModelExpanded(IVariableDeclarationModel model, bool expanded)
        {
            bool isExpanded = GetVariableDeclarationModelExpanded(model);
            if (isExpanded && !expanded)
            {
                m_BlackboardExpandedRowStates?.Remove(model.Guid.ToString());
            }
            else if (!isExpanded && expanded)
            {
                m_BlackboardExpandedRowStates?.Add(model.Guid.ToString());
            }
        }

        public bool GetVariableDeclarationModelExpanded(IVariableDeclarationModel model)
        {
            return m_BlackboardExpandedRowStates?.Contains(model.Guid.ToString()) ?? false;
        }
    }
}
