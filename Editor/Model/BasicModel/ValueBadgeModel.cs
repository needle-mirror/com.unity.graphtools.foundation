using System;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.BasicModel
{
    /// <summary>
    /// A model for values displayed in a badge.
    /// </summary>
    [Serializable]
    public class ValueBadgeModel : BadgeModel, IValueBadgeModel
    {
        [SerializeField]
        string m_DisplayValue;

        /// <inheritdoc />
        public string DisplayValue => m_DisplayValue;

        /// <inheritdoc />
        public IPortModel ParentPortModel => ParentModel as IPortModel;

        /// <summary>
        /// Initializes a new instance of the <see cref="ValueBadgeModel"/> class.
        /// </summary>
        public ValueBadgeModel(IPortModel parentModel, string value)
            : base(parentModel)
        {
            m_DisplayValue = value;
        }
    }
}
