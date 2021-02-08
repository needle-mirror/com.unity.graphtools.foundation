using System;
using System.Collections.Generic;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    interface IGraphViewSelection
    {
        int Version { get; set; }

        HashSet<string> SelectedElements { get; }
    }
}
