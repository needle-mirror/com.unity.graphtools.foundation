using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEditor.GraphToolsFoundation.Overdrive.BasicModel
{
    [Serializable]
    //[MovedFrom(false, "UnityEditor.VisualScripting.GraphViewModel", "Unity.GraphTools.Foundation.Overdrive.Editor")]
    [MovedFrom("UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.GraphViewModel")]
    public class PlacematModel : IPlacematModel, ISerializationCallbackReceiver, IGuidUpdate
    {
        public static readonly Color k_DefaultColor = new Color(0.15f, 0.19f, 0.19f);

        [SerializeField]
        string m_Title;

        [SerializeField]
        Rect m_Position;

        [SerializeField]
        Color m_Color;

        [SerializeField]
        bool m_Collapsed;

        [SerializeField]
        int m_ZOrder;

        [SerializeField]
        List<string> m_HiddenElements;

        [SerializeField]
        GraphAssetModel m_AssetModel;

        [SerializeField]
        SerializableGUID m_Guid;

        [SerializeField, Obsolete]
        string m_Id = "";

        [SerializeField]
        List<string> m_SerializedCapabilities;

        List<IGraphElementModel> m_CachedHiddenElementModels;

        protected List<Capabilities> m_Capabilities;

        public IGraphAssetModel AssetModel
        {
            get => m_AssetModel;
            set => m_AssetModel = (GraphAssetModel)value;
        }

        public virtual IGraphModel GraphModel => m_AssetModel.GraphModel;

        public string Title
        {
            get => m_Title;
            set => m_Title = value;
        }

        public string DisplayTitle => Title;

        public Rect PositionAndSize
        {
            get => m_Position;
            set
            {
                var r = value;
                if (!this.IsResizable())
                    r.size = m_Position.size;

                if (!this.IsMovable())
                    r.position = m_Position.position;

                m_Position = r;
            }
        }

        public Vector2 Position
        {
            get => PositionAndSize.position;
            set
            {
                if (!this.IsMovable())
                    return;

                PositionAndSize = new Rect(value, PositionAndSize.size);
            }
        }

        public Color Color
        {
            get => m_Color;
            set => m_Color = value;
        }

        public bool Collapsed
        {
            get => m_Collapsed;
            set
            {
                if (!this.IsCollapsible())
                    return;

                m_Collapsed = value;
                this.SetCapability(Overdrive.Capabilities.Resizable, !m_Collapsed);
            }
        }

        public int ZOrder
        {
            get => m_ZOrder;
            set => m_ZOrder = value;
        }

        public List<string> HiddenElementsGuid
        {
            get => m_HiddenElements;
            set
            {
                m_HiddenElements = value;
                m_CachedHiddenElementModels = null;
            }
        }

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

        public bool Destroyed { get; private set; }

        public PlacematModel()
        {
            InternalInitCapabilities();
            Color = k_DefaultColor;
        }

        public void Destroy() => Destroyed = true;

        public void Move(Vector2 delta)
        {
            if (!this.IsMovable())
                return;

            PositionAndSize = new Rect(PositionAndSize.position + delta, PositionAndSize.size);
        }

        public void Rename(string newName)
        {
            if (!this.IsRenamable())
                return;

            Title = newName;
        }

        public void ResetColor()
        {
            Color = k_DefaultColor;
        }

        public void AssignNewGuid()
        {
            m_Guid = GUID.Generate();
        }

        void IGuidUpdate.AssignGuid(string guidString)
        {
            m_Guid = new GUID(guidString);
            if (m_Guid.GUID.Empty())
                AssignNewGuid();
        }

        public virtual IReadOnlyList<Capabilities> Capabilities => m_Capabilities;

        public void OnBeforeSerialize()
        {
            m_SerializedCapabilities = m_Capabilities?.Select(c => c.Name).ToList() ?? new List<string>();
        }

        public void OnAfterDeserialize()
        {
            if (m_Guid.GUID.Empty())
            {
#pragma warning disable 612
                if (!String.IsNullOrEmpty(m_Id))
                {
                    (GraphModel as GraphModel)?.AddGuidToUpdate(this, m_Id);
                }
#pragma warning restore 612
            }

            if (!m_SerializedCapabilities.Any())
                // If we're reloading an older node
                InitCapabilities();
            else
                m_Capabilities = m_SerializedCapabilities.Select(Overdrive.Capabilities.Get).ToList();
        }

        internal void UpdateHiddenGuids(Dictionary<string, IGuidUpdate> mapping)
        {
            List<string> updatedHiddenElementGuids = new List<string>();
            bool updated = false;

            foreach (var hiddenGuid in HiddenElementsGuid)
            {
                if (mapping.TryGetValue(hiddenGuid, out var element) && element is IGraphElementModel graphElementModel)
                {
                    updated = true;
                    updatedHiddenElementGuids.Add(graphElementModel.Guid.ToString());
                }
                else
                {
                    updatedHiddenElementGuids.Add(hiddenGuid);
                }
            }

            if (updated)
                HiddenElementsGuid = updatedHiddenElementGuids;
        }

        public IEnumerable<IGraphElementModel> HiddenElements
        {
            get
            {
                if (m_CachedHiddenElementModels == null)
                {
                    if (HiddenElementsGuid != null)
                    {
                        m_CachedHiddenElementModels = new List<IGraphElementModel>();
                        foreach (var elementModelGuid in HiddenElementsGuid)
                        {
                            foreach (var node in GraphModel.NodeModels)
                            {
                                if (node.Guid.ToString() == elementModelGuid)
                                {
                                    m_CachedHiddenElementModels.Add(node);
                                }
                            }

                            foreach (var sticky in GraphModel.StickyNoteModels)
                            {
                                if (sticky.Guid.ToString() == elementModelGuid)
                                {
                                    m_CachedHiddenElementModels.Add(sticky);
                                }
                            }

                            foreach (var placemat in GraphModel.PlacematModels)
                            {
                                if (placemat.Guid.ToString() == elementModelGuid)
                                {
                                    m_CachedHiddenElementModels.Add(placemat);
                                }
                            }
                        }
                    }
                }

                return m_CachedHiddenElementModels ?? Enumerable.Empty<IGraphElementModel>();
            }
            set
            {
                if (value == null)
                {
                    m_HiddenElements = null;
                }
                else
                {
                    m_HiddenElements = new List<string>(value.Select(e => e.Guid.ToString()));
                }

                m_CachedHiddenElementModels = null;
            }
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
                Overdrive.Capabilities.Copiable,
                Overdrive.Capabilities.Selectable,
                Overdrive.Capabilities.Renamable,
                Overdrive.Capabilities.Movable,
                Overdrive.Capabilities.Resizable,
                Overdrive.Capabilities.Collapsible
            };
        }
    }
}
