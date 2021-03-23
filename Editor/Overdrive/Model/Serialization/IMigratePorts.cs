using System;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public interface IMigratePorts
    {
        bool MigratePort(ref string portReferenceUniqueId, Direction direction);
    }
}
