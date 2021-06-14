using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.BasicModel
{
    /// <summary>
    /// Constant model for <see cref="Quaternion"/>.
    /// </summary>
    public class QuaternionConstant : Constant<Quaternion>
    {
        public override object DefaultValue => Quaternion.identity;
    }
}
