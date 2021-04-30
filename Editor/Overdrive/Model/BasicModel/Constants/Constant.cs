using System;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEditor.GraphToolsFoundation.Overdrive.BasicModel
{
    [Serializable]
    [MovedFrom(false, sourceAssembly: "Unity.GraphTools.Foundation.Overdrive.Editor")]
    public abstract class Constant<T> : IConstant
    {
        [SerializeField]
        protected T m_Value;

        public T Value
        {
            get => m_Value;
            set => m_Value = value;
        }

        public object ObjectValue
        {
            get => m_Value;
            set => m_Value = FromObject(value);
        }

        public virtual object DefaultValue => default(T);

        public virtual Type Type => typeof(T);

        protected virtual T FromObject(object value) => (T)value;
    }
}
