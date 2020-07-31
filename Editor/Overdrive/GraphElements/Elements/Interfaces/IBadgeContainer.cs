using System;
using UnityEditor.GraphToolsFoundation.Overdrive.GraphElements;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public interface IBadgeContainer
    {
        IconBadge ErrorBadge { get; set; }
        ValueBadge ValueBadge { get; set; }
    }
}
