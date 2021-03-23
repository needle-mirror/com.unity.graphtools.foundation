#if UNITY_2020_1_OR_NEWER
using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.VisualScripting.GraphViewModel
{
    [Serializable]
    public class PlacematModel : IPlacematModel
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

        [SerializeField]
        string m_Id = Guid.NewGuid().ToString();

        [SerializeField]
        Rect m_Position;
        public Rect Position
        {
            get => m_Position;
            set => m_Position = value;
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

        [SerializeField]
        List<string> m_HiddenElements;
        public List<string> HiddenElementsGuid
        {
            get => m_HiddenElements;
            set => m_HiddenElements = value;
        }

        [SerializeField]
        GraphAssetModel m_AssetModel;
        public CapabilityFlags Capabilities => CapabilityFlags.Selectable | CapabilityFlags.Deletable | CapabilityFlags.Movable | CapabilityFlags.Copiable;
        public ScriptableObject SerializableAsset => (ScriptableObject)AssetModel;
        public IGraphAssetModel AssetModel => m_AssetModel;
        public IGraphModel GraphModel
        {
            get => m_AssetModel.GraphModel;
            set => m_AssetModel = value?.AssetModel as GraphAssetModel;
        }

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
            Position = new Rect(Position.position + delta, Position.size);
        }

        public void Move(Rect newRect)
        {
            Position = newRect;
        }

        public bool Destroyed { get; private set; }

        public void Destroy() => Destroyed = true;
    }
}
#endif
