using System;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.GraphToolsFoundation.Overdrive.BasicModel
{
    /// <summary>
    /// Constant model for enums.
    /// </summary>
    [Serializable]
    public class EnumConstant : Constant<EnumValueReference>
    {
        /// <inheritdoc />
        public override object DefaultValue => new EnumValueReference(EnumType);

        /// <summary>
        /// The constant value as an <see cref="Enum"/>.
        /// </summary>
        public Enum EnumValue => Value.ValueAsEnum();

        /// <summary>
        /// The <see cref="TypeHandle"/> for the type of the enum.
        /// </summary>
        public TypeHandle EnumType => Value.EnumType;

        /// <inheritdoc />
        protected override EnumValueReference FromObject(object value)
        {
            if (value is Enum e)
                return new EnumValueReference(e);
            return base.FromObject(value);
        }
    }
}
