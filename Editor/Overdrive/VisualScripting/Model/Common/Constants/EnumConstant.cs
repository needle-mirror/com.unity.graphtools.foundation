using System;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting
{
    [Serializable]
    [MovedFrom(false, "UnityEditor.VisualScripting.Model", "Unity.GraphTools.Foundation.Overdrive.Editor")]
    public class EnumConstant : Constant<EnumValueReference>
    {
        protected override EnumValueReference FromObject(object value)
        {
            if (value is Enum e)
                return new EnumValueReference(e);
            return base.FromObject(value);
        }

        public override object DefaultValue => new EnumValueReference(EnumType);

        public Enum EnumValue => Value.ValueAsEnum();

        public TypeHandle EnumType => Value.EnumType;
    }
}
