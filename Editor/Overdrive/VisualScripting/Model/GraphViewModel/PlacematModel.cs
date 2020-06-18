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
    public class PlacematModel : IPlacematModel, IGTFPlacematModel
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
        string m_Id = Guid.NewGuid().ToString();

        public void AssignNewGuid()
        {
            m_Id = Guid.NewGuid().ToString();
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
                            foreach (var node in VSGraphModel.NodeModels)
                            {
                                if (node.GetId() == elementModelGuid)
                                {
                                    m_CachedHiddenElementModels.Add(node as IGTFGraphElementModel);
                                }
                            }

                            foreach (var sticky in VSGraphModel.StickyNoteModels)
                            {
                                if (sticky.GetId() == elementModelGuid)
                                {
                                    m_CachedHiddenElementModels.Add(sticky as IGTFGraphElementModel);
                                }
                            }

                            foreach (var placemat in VSGraphModel.PlacematModels)
                            {
                                if (placemat.GetId() == elementModelGuid)
                                {
                                    m_CachedHiddenElementModels.Add(placemat as IGTFGraphElementModel);
                                }
                            }
                        }
                    }
                }

                return m_CachedHiddenElementModels;
            }
            set
            {
                if (value == null)
                {
                    m_HiddenElements = null;
                }
                else
                {
                    m_HiddenElements = new List<string>(value.OfType<IGraphElementModelWithGuid>().Select(e => e.GetId()));
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
        public ScriptableObject SerializableAsset => (ScriptableObject)AssetModel;
        public IGraphAssetModel AssetModel => m_AssetModel;
        public IGraphModel VSGraphModel
        {
            get => m_AssetModel.GraphModel;
            set => m_AssetModel = value?.AssetModel as GraphAssetModel;
        }

        public IGTFGraphModel GraphModel => VSGraphModel as IGTFGraphModel;

        public string GetId()
        {
            return m_Id;
        }

        public void UndoRedoPerformed()  // TODO needed?
        {
            throw new NotImplementedException();
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
