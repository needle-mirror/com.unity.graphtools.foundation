using System;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEditor.GraphToolsFoundation.Overdrive.BasicModel
{
    [Serializable]
    //[MovedFrom(false, "Unity.GraphToolsFoundation.Model", "Unity.GraphTools.Foundation.Overdrive.Editor")]
    [MovedFrom("UnityEditor.GraphToolsFoundation.Overdrive.Model")]
    public class EdgeControlPointModel : IEdgeControlPointModel
    {
        [SerializeField]
        Vector2 m_Position;

        [SerializeField]
        float m_Tightness;

        public Vector2 Position
        {
            get => m_Position;
            set => m_Position = value;
        }

        public float Tightness
        {
            get => m_Tightness;
            set => m_Tightness = value;
        }
    }
}
