using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Plugins.Debugging
{
    [MovedFrom(false, "UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.Plugins")]
    public class DebuggingValueBadgeModel : ValueBadgeModel
    {
        public DebuggingValueBadgeModel(TracingStep step)
            : base(step.PortModel, step.ValueString)
        {
        }
    }
}
