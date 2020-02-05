using System;
using UnityEditor.VisualScripting.Model;
using UnityEngine;

namespace UnityEditor.VisualScripting.GraphViewModel
{
    public interface IGraphElementModelWithGuid : IGraphElementModel
    {
        void AssignNewGuid();
    }

    public interface ICloneable : IGraphElementModel
    {
        IGraphElementModel Clone();
    }

    public interface IGraphElementModel : ICapabilitiesModel
    {
        ScriptableObject SerializableAsset { get; }
        IGraphAssetModel AssetModel { get; }
        IGraphModel GraphModel { get; }

        // TODO replace with GUID everywhere and merge IGraphElementModelWithGuid
        string GetId();
    }

    public static class GraphElementModelExtensions
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
                if (copy is IGraphElementModelWithGuid copyGuid)
                    copyGuid.AssignNewGuid();
                return copy;
            }

            return CloneUsingScriptableObjectInstantiate(element);
        }

        public static T CloneUsingScriptableObjectInstantiate<T>(T element) where T : IGraphElementModel
        {
            Holder h = ScriptableObject.CreateInstance<Holder>();
            h.model = element;

            // TODO: wait for CopySerializedManagedFieldsOnly to be able to copy plain c# objects with [SerializeReference] fields
//            var clone = (T)Activator.CreateInstance(element.GetType());
//            EditorUtility.CopySerializedManagedFieldsOnly(element, clone);
            var h2 = ScriptableObject.Instantiate(h);
            var clone = h2.model;

            if (clone is IGraphElementModelWithGuid hasGuid)
                hasGuid.AssignNewGuid();

            ScriptableObject.DestroyImmediate(h);
            ScriptableObject.DestroyImmediate(h2);
            return (T)clone;
        }
    }

    public interface IUndoRedoAware
    {
        void UndoRedoPerformed();
    }
}
