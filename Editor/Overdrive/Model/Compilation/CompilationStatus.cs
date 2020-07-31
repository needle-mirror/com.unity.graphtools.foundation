using System;
using JetBrains.Annotations;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    [PublicAPI]
    public enum CompilationStatus
    {
        Succeeded,
        Restart,
        Failed
    }
}
