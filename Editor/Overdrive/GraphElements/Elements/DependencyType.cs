using System;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    [Flags]
    public enum DependencyType
    {
        None = 0,
        Style = 1,
        Geometry = 2,
        Removal = 4,
    }

    public static class DependencyTypeExtensions
    {
        public static bool HasFlagFast(this DependencyType value, DependencyType flag)
        {
            return (value & flag) != 0;
        }
    }
}
