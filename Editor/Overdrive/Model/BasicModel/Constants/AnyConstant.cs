using System;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEditor.GraphToolsFoundation.Overdrive.BasicModel
{
    [MovedFrom(false, sourceAssembly: "Unity.GraphTools.Foundation.Overdrive.Editor")]
    public class AnyConstant : Constant<object>
    {
        public override Type Type => Value != null ? Value.GetType() : typeof(object);
    }
}
