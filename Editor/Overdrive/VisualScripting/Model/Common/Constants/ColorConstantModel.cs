using System;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting
{
    [Serializable]
    [MovedFrom(false, "UnityEditor.VisualScripting.Model", "Unity.GraphTools.Foundation.Overdrive.Editor")]
    public class ColorConstantModel : ConstantNodeModel<Color>
    {
        protected override Color DefaultValue => new Color(0, 0, 0, 1);
        public override string Title => string.Empty;
    }
}
