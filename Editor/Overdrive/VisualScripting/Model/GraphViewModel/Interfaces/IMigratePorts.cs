using System;
using UnityEditor.GraphToolsFoundation.Overdrive.GraphElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.GraphViewModel
{
    public interface IMigratePorts
    {
        bool MigratePort(ref string portReferenceUniqueId, Direction direction);
    }
}
