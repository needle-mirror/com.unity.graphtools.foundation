using System;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.BasicModel
{
    [Serializable]
    public class ValueBadgeModel : BadgeModel, IValueBadgeModel
    {
        [SerializeField]
        string m_DisplayValue;

        public string DisplayValue => m_DisplayValue;

        public IPortModel ParentPortModel => ParentModel as IPortModel;

        public ValueBadgeModel(IPortModel parentModel, string value)
            : base(parentModel)
        {
            m_DisplayValue = value;
        }
    }
}
