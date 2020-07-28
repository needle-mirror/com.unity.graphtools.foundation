using System;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting
{
    // TODO theor remake a CNM for migration

    [Serializable]
    [MovedFrom(false, "UnityEditor.VisualScripting.Model", "Unity.GraphTools.Foundation.Overdrive.Editor")]
    public struct InputName
    {
        public string name;

        public override string ToString()
        {
            return String.IsNullOrEmpty(name) ? "<No Input>" : name;
        }
    }
}
