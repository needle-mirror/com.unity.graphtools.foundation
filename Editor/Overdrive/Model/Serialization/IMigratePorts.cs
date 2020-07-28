using System;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Model
{
    public interface IMigratePorts
    {
        bool MigratePort(ref string portReferenceUniqueId, Direction direction);
    }
}
