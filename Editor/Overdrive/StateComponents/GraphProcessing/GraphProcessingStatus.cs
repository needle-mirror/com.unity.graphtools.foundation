using System;
using JetBrains.Annotations;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    [PublicAPI]
    public enum GraphProcessingStatus
    {
        Succeeded,
        Restart,
        Failed
    }
}
