using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEditor.GraphToolsFoundation.Overdrive.BasicModel
{
    [MovedFrom("UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting")]
    public class QuaternionConstant : Constant<Quaternion>
    {
        public override object DefaultValue => Quaternion.identity;
    }
}
