using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Plugins.Debugging
{
    [MovedFrom(false, "UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.Plugins")]
    public class DebuggingErrorBadgeModel : ErrorBadgeModel
    {
        public DebuggingErrorBadgeModel(TracingStep step)
            : base(step.NodeModel)
        {
            m_ErrorMessage = step.ErrorMessage;
        }
    }
}
