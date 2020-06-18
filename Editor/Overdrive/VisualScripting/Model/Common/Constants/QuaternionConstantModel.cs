using System;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting
{
    [Serializable]
    [MovedFrom(false, "UnityEditor.VisualScripting.Model", "Unity.GraphTools.Foundation.Overdrive.Editor")]
    public class QuaternionConstantModel : ConstantNodeModel<Quaternion>
    {
        protected override Quaternion DefaultValue => Quaternion.identity;
    }
}
