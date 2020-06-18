using System;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting
{
    /** Event and field accessibility modifier
    * */
    [Serializable]
    [MovedFrom(false, "UnityEditor.VisualScripting.Model", "Unity.GraphTools.Foundation.Overdrive.Editor")]
    [PublicAPI]
    [Flags]
    public enum AccessibilityFlags
    {
        Default = 0,       // No particular accessibility specified, ie void Thing();
        Public = 1,
        Protected = 2,
        Private = 4,
        Static = 8,
        Override = 16
    }
}
