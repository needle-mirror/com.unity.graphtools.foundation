using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEditor.GraphToolsFoundation.Overdrive.BasicModel
{
    [Serializable]
    //[MovedFrom(false, "UnityEditor.VisualScripting.GraphViewModel", "Unity.GraphTools.Foundation.Overdrive.Editor")]
    [MovedFrom("UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.GraphViewModel")]
    public class PlacematModel : IGTFPlacematModel, ISerializationCallbackReceiver, IGuidUpdate
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

        List<IGTFGraphElementModel> m_CachedHiddenElementModels;

        public IGTFGraphAssetModel AssetModel
        {
            get => m_AssetModel;
            set => m_AssetModel = (GraphAssetModel)value;
        }

        public IGTFGraphModel GraphModel => m_AssetModel.GraphModel;

        public string Title
        {
            get => m_Title;
            set => m_Title = value;
        }

        public string DisplayTitle => Title;

        public bool IsRenamable => true;

        public bool IsResizable => !Collapsed;

        public Rect PositionAndSize
        {
            get => m_Position;
            set => m_Position = value;
        }

        public Vector2 Position
        {
            get => PositionAndSize.position;
            set => PositionAndSize = new Rect(value, PositionAndSize.size);
        }

        public Color Color
        {
            get => m_Color;
            set => m_Color = value;
        }

        public bool Collapsed
        {
            get => m_Collapsed;
            set => m_Collapsed = value;
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

        public bool IsDeletable => true;

        public bool IsCopiable => true;

        public PlacematModel()
        {
            Color = k_DefaultColor;
        }

        public void Destroy() => Destroyed = true;

        public void Move(Vector2 delta)
        {
            PositionAndSize = new Rect(PositionAndSize.position + delta, PositionAndSize.size);
        }

        public void Rename(string newName)
        {
            Title = newName;
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
    }
}
