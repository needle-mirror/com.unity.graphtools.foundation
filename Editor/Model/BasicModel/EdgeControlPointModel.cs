using System;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEditor.GraphToolsFoundation.Overdrive.BasicModel
{
    [Serializable]
    [MovedFrom(false, sourceAssembly: "Unity.GraphTools.Foundation.Overdrive.Editor")]
    public class EdgeControlPointModel : IEdgeControlPointModel
    {
        [SerializeField]
        Vector2 m_Position;

        [SerializeField]
        float m_Tightness;

        /// <inheritdoc />
        public Vector2 Position
        {
            get => m_Position;
            set => m_Position = value;
        }

        /// <inheritdoc />
        public float Tightness
        {
            get => m_Tightness;
            set => m_Tightness = value;
        }
    }
}
