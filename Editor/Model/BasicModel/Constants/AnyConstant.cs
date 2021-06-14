using System;

namespace UnityEditor.GraphToolsFoundation.Overdrive.BasicModel
{
    /// <summary>
    /// Constant model for objects of any type.
    /// </summary>
    public class AnyConstant : Constant<object>
    {
        /// <inheritdoc />
        public override Type Type => Value != null ? Value.GetType() : typeof(object);
    }
}
