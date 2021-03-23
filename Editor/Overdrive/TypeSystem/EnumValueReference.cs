using System;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    [Serializable]
    //[MovedFrom(false, "UnityEditor.VisualScripting.Model", "Unity.GraphTools.Foundation.Overdrive.Editor")]
    [MovedFrom(false, "UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting", "Unity.GraphTools.Foundation.Overdrive.Editor")]
    public struct EnumValueReference
    {
        //TODO Figure how we handle enum types
        [SerializeField]
        TypeHandle m_EnumType;
        [SerializeField]
        int m_Value;

        public EnumValueReference(TypeHandle handle)
        {
            m_EnumType = handle;
            m_Value = 0;
        }

        public EnumValueReference(Enum e)
        {
            m_EnumType = e.GetType().GenerateTypeHandle();
            m_Value = Convert.ToInt32(e);
        }

        public TypeHandle EnumType
        {
            get => m_EnumType;
            set
            {
                m_EnumType = value;
                m_Value = 0;
            }
        }

        public Enum ValueAsEnum()
        {
            return (Enum)Enum.ToObject(m_EnumType.Resolve(), m_Value);
        }

        public int Value
        {
            get => m_Value;
            set => m_Value = value;
        }

        public bool IsValid() => m_EnumType.IsValid && m_EnumType.Resolve().IsEnum;
    }
}
