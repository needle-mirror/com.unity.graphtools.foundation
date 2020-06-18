using System;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting
{
    [Serializable]
    [MovedFrom(false, "UnityEditor.VisualScripting.Model", "Unity.GraphTools.Foundation.Overdrive.Editor")]
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

        public TypeHandle EnumType
        {
            get => m_EnumType;
            set
            {
                m_EnumType = value;
                m_Value = 0;
            }
        }

        public Enum ValueAsEnum(Stencil stencil)
        {
            return (Enum)Enum.ToObject(m_EnumType.Resolve(stencil), m_Value);
        }

        public int Value
        {
            get => m_Value;
            set => m_Value = value;
        }

        public bool IsValid(Stencil stencil) => m_EnumType.IsValid && m_EnumType.Resolve(stencil).IsEnum;
    }
}
