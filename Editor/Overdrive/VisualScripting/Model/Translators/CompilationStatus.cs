using System;
using JetBrains.Annotations;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting
{
    [PublicAPI]
    public enum CompilationStatus
    {
        Succeeded,
        Restart,
        Failed
    }
}
