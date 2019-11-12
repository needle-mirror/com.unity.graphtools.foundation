using System;
using JetBrains.Annotations;

namespace UnityEngine.VisualScripting
{
    [Flags]
    [PublicAPI]
    public enum CompilationOptions
    {
        Default              = 0,
        Tracing              = 1 << 0,
        Profiling            = 1 << 1,
        LiveEditing          = 1 << 2,
        ImplementationOnly   = 1 << 3
    }

    public enum SourceCodePhases
    {
        Initial,
        Final,
    }
}
