#if DISABLE_SIMPLE_MATH_TESTS
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
struct JSONSerializedElement
{
    [SerializeField]
    public string typeName;

    [SerializeField]
    public string data;
}

[Serializable]
class CopyPasteData<T> : ISerializationCallbackReceiver where T : class
{
    [NonSerialized]
    List<T> m_ScriptableObjects;

    [SerializeField]
    List<JSONSerializedElement> m_SerializedElements = new List<JSONSerializedElement>();

    public CopyPasteData(IEnumerable<T> elements)
    {
        m_ScriptableObjects = new List<T>(elements);
    }

    public IReadOnlyList<MathNode> GetNodes() => m_ScriptableObjects.OfType<MathNode>().ToList();
    public IReadOnlyList<MathPlacemat> GetPlacemats() => m_ScriptableObjects.OfType<MathPlacemat>().ToList();

    public virtual void OnBeforeSerialize()
    {
        m_SerializedElements = new List<JSONSerializedElement>();
        foreach (var scriptableObject in m_ScriptableObjects)
        {
            if (scriptableObject  == null)
                continue;

            string typeName = scriptableObject.GetType().AssemblyQualifiedName;
            string data = JsonUtility.ToJson(scriptableObject);

            m_SerializedElements.Add(new JSONSerializedElement { typeName = typeName, data = data });
        }
    }

    public virtual void OnAfterDeserialize()
    {
        m_ScriptableObjects = new List<T>(m_SerializedElements.Count);
        foreach (JSONSerializedElement e in m_SerializedElements)
        {
            var t = Type.GetType(e.typeName);
            T instance = ScriptableObject.CreateInstance(t) as T;
            if (instance != null)
            {
                JsonUtility.FromJsonOverwrite(e.data, instance);
                m_ScriptableObjects.Add(instance);
            }
        }
        m_SerializedElements = null;
    }
}
#endif
