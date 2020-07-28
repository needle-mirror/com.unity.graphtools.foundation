using System;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEditor.GraphToolsFoundation.Overdrive.BasicModel
{
    [Serializable]
    [MovedFrom("UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting")]
    public class EnumConstant : Constant<EnumValueReference>
    {
        public override object DefaultValue => new EnumValueReference(EnumType);

        public Enum EnumValue => Value.ValueAsEnum();

        public TypeHandle EnumType => Value.EnumType;

        protected override EnumValueReference FromObject(object value)
        {
            if (value is Enum e)
                return new EnumValueReference(e);
            return base.FromObject(value);
        }
    }
}
