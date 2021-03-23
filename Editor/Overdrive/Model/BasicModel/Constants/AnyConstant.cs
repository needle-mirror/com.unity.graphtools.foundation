using System;

namespace UnityEditor.GraphToolsFoundation.Overdrive.BasicModel
{
    public class AnyConstant : Constant<object>
    {
        public override Type Type => Value != null ? Value.GetType() : typeof(object);
    }
}
