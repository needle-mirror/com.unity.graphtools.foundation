using System;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.Plugins
{
    public class DebuggingValueBadgeModel : ValueBadgeModel
    {
        public DebuggingValueBadgeModel(TracingStep step)
            : base(step.PortModel, step.ValueString)
        {
        }
    }
}
