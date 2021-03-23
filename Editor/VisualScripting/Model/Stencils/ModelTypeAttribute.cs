using System;

namespace UnityEditor.VisualScripting.Model.Stencils
{
    [AttributeUsage(AttributeTargets.Method)]
    public class ModelType : Attribute
    {
        public Type Type { get; set; }
    }
}
