using System;
using UnityEngine;

namespace UnityEditor.VisualScripting.Model
{
    [Serializable]
    public class ColorConstantModel : ConstantNodeModel<Color>
    {
        protected override Color DefaultValue => new Color(0, 0, 0, 1);
        public override string Title => string.Empty;
    }
}
