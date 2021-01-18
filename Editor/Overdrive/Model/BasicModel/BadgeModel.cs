using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.BasicModel
{
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

        public GUID Guid
        {
            get
            {
                if (m_Guid.GUID.Empty())
                    AssignNewGuid();
                return m_Guid;
            }
            set => m_Guid = value;
        }

        public IGraphElementModel ParentModel { get; }

        public void AssignNewGuid()
        {
            m_Guid = GUID.Generate();
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
