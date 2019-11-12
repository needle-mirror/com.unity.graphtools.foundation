using System;

namespace UnityEditor.VisualScripting.Editor
{
    [Flags]
    public enum UpdateFlags
    {
        None               = 0,
        Selection          = 1 << 0,
        GraphGeometry      = 1 << 1,
        GraphTopology      = 1 << 2,
        CompilationResult  = 1 << 3,
        RequestCompilation = 1 << 4,
        RequestRebuild     = 1 << 5,

        All = Selection | GraphGeometry | GraphTopology | RequestCompilation | RequestRebuild,
    }
}
