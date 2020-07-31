using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting
{
    public class QuaternionConstant : Constant<Quaternion>
    {
        public override object DefaultValue => Quaternion.identity;
    }
}
