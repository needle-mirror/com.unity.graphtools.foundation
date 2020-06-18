using System;
using UnityEditor.GraphToolsFoundation.Overdrive.GraphElements;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting
{
    public interface IBadgeContainer
    {
        IconBadge ErrorBadge { get; set; }
        ValueBadge ValueBadge { get; set; }
    }
}
