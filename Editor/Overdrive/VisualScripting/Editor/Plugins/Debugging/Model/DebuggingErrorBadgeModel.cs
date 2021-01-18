using System;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.Plugins
{
    public class DebuggingErrorBadgeModel : ErrorBadgeModel
    {
        public DebuggingErrorBadgeModel(TracingStep step)
            : base(step.NodeModel)
        {
            m_ErrorMessage = step.ErrorMessage;
        }
    }
}
