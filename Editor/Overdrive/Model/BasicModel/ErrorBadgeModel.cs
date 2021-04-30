using System;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEditor.GraphToolsFoundation.Overdrive.BasicModel
{
    [Serializable]
    [MovedFrom(false, sourceAssembly: "Unity.GraphTools.Foundation.Overdrive.Editor")]
    public class ErrorBadgeModel : BadgeModel, IErrorBadgeModel
    {
        [SerializeField]
        protected string m_ErrorMessage;

        public string ErrorMessage => m_ErrorMessage;

        public ErrorBadgeModel(IGraphElementModel parentModel)
            : base(parentModel) { }
    }
}
