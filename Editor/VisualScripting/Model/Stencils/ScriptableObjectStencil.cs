using System;
using UnityEngine;

namespace UnityEditor.VisualScripting.Model.Stencils
{
    class ScriptableObjectStencil : ClassStencil
    {
        public override Type GetBaseClass()
        {
            return typeof(ScriptableObject);
        }
    }
}
