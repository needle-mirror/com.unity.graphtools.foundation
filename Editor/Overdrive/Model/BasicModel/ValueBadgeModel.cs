using System;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEditor.GraphToolsFoundation.Overdrive.BasicModel
{
    [Serializable]
    [MovedFrom(false, sourceAssembly: "Unity.GraphTools.Foundation.Overdrive.Editor")]
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
