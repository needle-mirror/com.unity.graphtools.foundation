using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.BasicModel
{
    /// <summary>
    /// A model that represents a badge in a graph.
    /// </summary>
    [Serializable]
    public class BadgeModel : IBadgeModel
    {
        public IGraphModel GraphModel => ParentModel.GraphModel;

        [SerializeField]
        SerializableGUID m_Guid;

        [SerializeField]
        IGraphAssetModel m_AssetModel;

        protected List<Capabilities> m_Capabilities;
        public IReadOnlyList<Capabilities> Capabilities => m_Capabilities ?? (m_Capabilities = new List<Capabilities> {Overdrive.Capabilities.NoCapabilities});

        /// <summary>
        /// The unique identifier of the badge.
        /// </summary>
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

        public IGraphElementModel ParentModel { get; }

        /// <summary>
        /// Assign a newly generated GUID to the model.
        /// </summary>
        public void AssignNewGuid()
        {
            m_Guid = SerializableGUID.Generate();
        }

        public IGraphAssetModel AssetModel
        {
            get => m_AssetModel;
            set => m_AssetModel = value;
        }

        public BadgeModel(IGraphElementModel parentModel)
        {
            ParentModel = parentModel;
        }
    }
}
