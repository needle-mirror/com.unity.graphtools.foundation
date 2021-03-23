using System;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    // ReSharper disable once InconsistentNaming
    static class IStateComponentExtension
    {
        internal static StateComponentVersion GetStateComponentVersion(this IStateComponent component)
        {
            return new StateComponentVersion
            {
                HashCode = component.GetHashCode(),
                Version = component.CurrentVersion
            };
        }
    }
}
