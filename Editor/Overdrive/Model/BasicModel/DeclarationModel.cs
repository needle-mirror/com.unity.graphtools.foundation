using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

namespace UnityEditor.GraphToolsFoundation.Overdrive.BasicModel
{
    [Serializable]
    public class DeclarationModel : IDeclarationModel, IRenamable, ISerializationCallbackReceiver, IGuidUpdate
    {
        [SerializeField]
        GraphAssetModel m_AssetModel;

        [SerializeField]
        string m_Id = "";

        [SerializeField]
        SerializableGUID m_Guid;

        [SerializeField]
        List<string> m_SerializedCapabilities;

        [FormerlySerializedAs("name")]
        [SerializeField]
        string m_Name;

        protected List<Capabilities> m_Capabilities;

        public IGraphAssetModel AssetModel
        {
            get => m_AssetModel;
            set => m_AssetModel = (GraphAssetModel)value;
        }

        public IGraphModel GraphModel => m_AssetModel ? m_AssetModel.GraphModel : null;

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

        public void AssignNewGuid()
        {
            if (!String.IsNullOrEmpty(m_Id))
            {
                if (GUID.TryParse(m_Id.Replace("-", null), out var migratedGuid))
                    m_Guid = migratedGuid;
                else
                {
                    Debug.Log("FAILED PARSING " + m_Id);
                    m_Guid = GUID.Generate();
                }
            }
            else
                m_Guid = GUID.Generate();
        }

        public IReadOnlyList<Capabilities> Capabilities => m_Capabilities;
        public string Title
        {
            get => m_Name;
            set => m_Name = value;
        }

        public virtual string DisplayTitle => Title.Nicify();


        public DeclarationModel()
        {
            InternalInitCapabilities();
        }

        public void Rename(string newName)
        {
            if (!this.IsRenamable())
                return;

            SetNameFromUserName(newName);
        }

        public void OnBeforeSerialize()
        {
            m_SerializedCapabilities = m_Capabilities?.Select(c => c.Name).ToList() ?? new List<string>();
        }

        public void OnAfterDeserialize()
        {
            if (m_Guid.GUID.Empty())
            {
                if (!String.IsNullOrEmpty(m_Id))
                {
                    (GraphModel as GraphModel)?.AddGuidToUpdate(this, m_Id);
                }
            }

            if (!m_SerializedCapabilities.Any())
                // If we're reloading an older node
                InitCapabilities();
            else
                m_Capabilities = m_SerializedCapabilities.Select(Overdrive.Capabilities.Get).ToList();
        }

        public void AssignGuid(string guidString)
        {
            m_Guid = new GUID(guidString);
            if (m_Guid.GUID.Empty())
                AssignNewGuid();
        }

        protected virtual void InitCapabilities()
        {
            InternalInitCapabilities();
        }

        void InternalInitCapabilities()
        {
            m_Capabilities = new List<Capabilities>
            {
                Overdrive.Capabilities.Deletable,
                Overdrive.Capabilities.Droppable,
                Overdrive.Capabilities.Copiable,
                Overdrive.Capabilities.Selectable,
                Overdrive.Capabilities.Renamable
            };
        }

        void SetNameFromUserName(string userName)
        {
            string newName = userName.ToUnityNameFormat();
            if (string.IsNullOrWhiteSpace(newName))
                return;

            Title = newName;
        }
    }
}
