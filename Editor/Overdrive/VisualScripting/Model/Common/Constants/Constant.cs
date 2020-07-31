using System;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting
{
    [Serializable]
    public abstract class Constant<T> : IConstant
    {
        public T Value;
        public object ObjectValue
        {
            get => Value;
            set => Value = FromObject(value);
        }

        protected virtual T FromObject(object value) => (T)value;

        public virtual object DefaultValue => default(T);
        public virtual Type Type => typeof(T);
    }
}
