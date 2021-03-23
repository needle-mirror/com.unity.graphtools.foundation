using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Serialization;

namespace UnityEditor.GraphToolsFoundation.Overdrive.BasicModel
{
    /// <summary>
    /// Base class for graph element models
    /// handles IGraphElementModel properties like guid and capabilities
    /// </summary>
    [Serializable]
    public abstract class GraphElementModel : IGraphElementModel, ISerializationCallbackReceiver
    {
        [SerializeField, HideInInspector]
        SerializableGUID m_Guid;

        [SerializeField, HideInInspector, FormerlySerializedAs("m_GraphAssetModel")]
        protected GraphAssetModel m_AssetModel;

        [SerializeField, HideInInspector]
        Color m_Color;

        [SerializeField, HideInInspector]
        bool m_HasUserColor;

        [SerializeField, HideInInspector]
        private SerializationVersion m_Version;

        /// <summary>
        /// Serialized version, used for backward compatibility
        /// </summary>
        public SerializationVersion Version => m_Version;

        /// <inheritdoc />
        public virtual IGraphModel GraphModel => AssetModel?.GraphModel;

        protected List<Capabilities> m_Capabilities;

        [SerializeField, HideInInspector]
        List<string> m_SerializedCapabilities;

        /// <inheritdoc />
        public SerializableGUID Guid
        {
            get
            {
                if (!m_Guid.Valid)
                    AssignNewGuid();
                return m_Guid;
            }
            set => m_Guid = value;
        }

        /// <inheritdoc />
        public virtual IGraphAssetModel AssetModel
        {
            get => m_AssetModel;
            set
            {
                Assert.IsNotNull(value);
                m_AssetModel = (GraphAssetModel)value;
            }
        }

        /// <inheritdoc />
        public virtual void AssignNewGuid()
        {
            m_Guid = SerializableGUID.Generate();
        }

        /// <inheritdoc />
        public IReadOnlyList<Capabilities> Capabilities => m_Capabilities ?? (m_Capabilities = new List<Capabilities> { Overdrive.Capabilities.NoCapabilities });

        /// <summary>
        /// Used for backward compatibility
        /// </summary>
        protected internal Color InternalSerializedColor => m_Color;

        /// <summary>
        /// Default Color to use when no user color is provided
        /// </summary>
        public virtual Color DefaultColor => Color.clear;

        /// <inheritdoc />
        public Color Color
        {
            get => HasUserColor ? m_Color : DefaultColor;
            set
            {
                if (this.IsColorable())
                {
                    m_HasUserColor = true;
                    m_Color = value;
                }
            }
        }

        /// <inheritdoc />
        public bool HasUserColor => m_HasUserColor;

        /// <inheritdoc />
        public void ResetColor()
        {
            m_HasUserColor = false;
        }

        /// <inheritdoc />
        public virtual void OnBeforeSerialize()
        {
            m_SerializedCapabilities = m_Capabilities?.Select(c => c.Name).ToList() ?? new List<string>();
            m_Version = SerializationVersion.Latest;
        }

        /// <inheritdoc />
        public virtual void OnAfterDeserialize()
        {
            // TODO Vlad: only keep 'else' clause after release 0.9
            if (!m_SerializedCapabilities.Any())
                // If we're reloading an older graphelement
                InitCapabilities();
            else
                m_Capabilities = m_SerializedCapabilities.Select(Overdrive.Capabilities.Get).ToList();
        }

        protected virtual void InitCapabilities()
        {
            m_Capabilities = new List<Capabilities>
            {
                Overdrive.Capabilities.NoCapabilities
            };
        }

        /// <summary>
        /// Value increasing every release of GTF.
        /// </summary>
        /// <remarks>
        /// Useful for models backward compatibility
        /// </remarks>
        public enum SerializationVersion
        {
            GTF_V_0_8_2 = 0,

            /// <summary>
            /// Keep Latest as the highest value in this enum
            /// </summary>
            Latest
        }
    }
}
