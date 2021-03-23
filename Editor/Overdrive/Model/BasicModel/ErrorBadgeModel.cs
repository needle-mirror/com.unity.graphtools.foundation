using System;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.BasicModel
{
    [Serializable]
    public class ErrorBadgeModel : BadgeModel, IErrorBadgeModel
    {
        [SerializeField]
        protected string m_ErrorMessage;

        public string ErrorMessage => m_ErrorMessage;

        public ErrorBadgeModel(IGraphElementModel parentModel)
            : base(parentModel) { }
    }
}
