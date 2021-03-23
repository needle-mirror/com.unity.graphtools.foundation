using System;
using JetBrains.Annotations;

namespace UnityEditor.VisualScripting.Model
{
    [PublicAPI]
    public enum CompilationStatus
    {
        Succeeded,
        Restart,
        Failed
    }
}
