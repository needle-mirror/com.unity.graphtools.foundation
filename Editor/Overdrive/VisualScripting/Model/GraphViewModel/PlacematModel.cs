using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.GraphViewModel
{
    [Serializable]
    [MovedFrom(false, "UnityEditor.VisualScripting.GraphViewModel", "Unity.GraphTools.Foundation.Overdrive.Editor")]
    public class PlacematModel : IGTFPlacematModel, IGTFGraphElementModel, ISerializationCallbackReceiver, IGuidUpdate
    {
        public static readonly Color k_DefaultColor = new Color(0.15f, 0.19f, 0.19f);

        public PlacematModel()
        {
            Color = k_DefaultColor;
        }

        [SerializeField]
        string m_Title;

        public string Title
        {
            get => m_Title;
            set => m_Title = value;
        }

        public string DisplayTitle => Title;

        public bool IsRenamable => true;
        public void Rename(string newName)
        {
            Title = newName;
        }

        [SerializeField]
        SerializableGUID m_Guid;

        public GUID Guid
        {
            get
            {
                if (m_Guid.GUID.Empty())
                    AssignNewGuid();
                return m_Guid;
            }
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

        [SerializeField, Obsolete]
        string m_Id = "";

        public void OnBeforeSerialize()
        {
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
        }

        internal void UpdateHiddenGuids(Dictionary<string, IGuidUpdate> mapping)
        {
            List<string> updatedHiddenElementGuids = new List<string>();
            bool updated = false;

            foreach (var hiddenGuid in HiddenElementsGuid)
            {
                if (mapping.TryGetValue(hiddenGuid, out var element) && element is IGTFGraphElementModel graphElementModel)
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

        [SerializeField]
        Rect m_Position;
        public Rect PositionAndSize
        {
            get => m_Position;
            set => m_Position = value;
        }

        public bool IsResizable => !Collapsed;

        public Vector2 Position
        {
            get => PositionAndSize.position;
            set => PositionAndSize = new Rect(value, PositionAndSize.size);
        }

        [SerializeField]
        Color m_Color;
        public Color Color
        {
            get => m_Color;
            set => m_Color = value;
        }

        [SerializeField]
        bool m_Collapsed;
        public bool Collapsed
        {
            get => m_Collapsed;
            set => m_Collapsed = value;
        }

        [SerializeField]
        int m_ZOrder;
        public int ZOrder
        {
            get => m_ZOrder;
            set => m_ZOrder = value;
        }

        List<IGTFGraphElementModel> m_CachedHiddenElementModels;
        public IEnumerable<IGTFGraphElementModel> HiddenElements
        {
            get
            {
                if (m_CachedHiddenElementModels == null)
                {
                    if (HiddenElementsGuid != null)
                    {
                        m_CachedHiddenElementModels = new List<IGTFGraphElementModel>();
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

                return m_CachedHiddenElementModels ?? Enumerable.Empty<IGTFGraphElementModel>();
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

        [SerializeField]
        List<string> m_HiddenElements;
        public List<string> HiddenElementsGuid
        {
            get => m_HiddenElements;
            set
            {
                m_HiddenElements = value;
                m_CachedHiddenElementModels = null;
            }
        }

        [SerializeField]
        GraphAssetModel m_AssetModel;
        public IGTFGraphAssetModel AssetModel => m_AssetModel;
        public IGTFGraphModel GraphModel
        {
            get => m_AssetModel.GraphModel;
            set => m_AssetModel = value?.AssetModel as GraphAssetModel;
        }

        public void Move(Vector2 delta)
        {
            PositionAndSize = new Rect(PositionAndSize.position + delta, PositionAndSize.size);
        }

        public void Move(Rect newRect)
        {
            PositionAndSize = newRect;
        }

        public bool Destroyed { get; private set; }

        public void Destroy() => Destroyed = true;

        public bool IsDeletable => true;
        public bool IsCopiable => true;
    }
}
