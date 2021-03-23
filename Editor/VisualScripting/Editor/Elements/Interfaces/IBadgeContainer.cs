using System;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace UnityEditor.VisualScripting.Editor
{
    public interface IBadgeContainer
    {
        IconBadge ErrorBadge { get; set; }
        ValueBadge ValueBadge { get; set; }
    }
}
