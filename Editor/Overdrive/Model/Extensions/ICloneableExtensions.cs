using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    // ReSharper disable once InconsistentNaming
    public static class ICloneableExtensions
    {
        class Holder : ScriptableObject
        {
            [SerializeReference]
            public IGraphElementModel model;
        }

        public static T Clone<T>(this T element) where T : IGraphElementModel
        {
            if (element is ICloneable cloneable)
            {
                T copy = (T)cloneable.Clone();
                copy.AssignNewGuid();
                return copy;
            }

            return CloneUsingScriptableObjectInstantiate(element);
        }

        public static T CloneConstant<T>(this T element) where T : IConstant
        {
            T copy = (T)Activator.CreateInstance(element.GetType());
            copy.ObjectValue = element.ObjectValue;
            return copy;
        }

        public static T CloneUsingScriptableObjectInstantiate<T>(T element) where T : IGraphElementModel
        {
            Holder h = ScriptableObject.CreateInstance<Holder>();
            h.model = element;

            // TODO: wait for CopySerializedManagedFieldsOnly to be able to copy plain c# objects with [SerializeReference] fields
            //            var clone = (T)Activator.CreateInstance(element.GetType());
            //            EditorUtility.CopySerializedManagedFieldsOnly(element, clone);
            var h2 = Object.Instantiate(h);
            var clone = h2.model;
            clone.AssignNewGuid();

            Object.DestroyImmediate(h);
            Object.DestroyImmediate(h2);
            return (T)clone;
        }
    }
}
