using System;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.GraphViewModel
{
    [Serializable]
    [MovedFrom(false, "UnityEditor.VisualScripting.GraphViewModel", "Unity.GraphTools.Foundation.Overdrive.Editor")]
    public sealed class StickyNoteModel : IStickyNoteModel, ISerializationCallbackReceiver, IGTFStickyNoteModel
    {
        [SerializeField]
        string m_Title;

        [SerializeField]
        string m_Id = Guid.NewGuid().ToString();

        public StickyNoteModel()
        {
            Title = string.Empty;
            Contents = string.Empty;
            Theme = StickyNoteColorTheme.Classic.ToString();
            TextSize = StickyNoteTextSize.Small.ToString();
            PositionAndSize = Rect.zero;
        }

        public string Title
        {
            get => m_Title;
            set { if (value != null && m_Title != value) m_Title = value; }
        }

        public string DisplayTitle => Title;

        [SerializeField]
        string m_Contents;
        public string Contents
        {
            get => m_Contents;
            set { if (value != null && m_Contents != value) m_Contents = value; }
        }

        [SerializeField]
#pragma warning disable 649
        StickyNoteColorTheme m_Theme;
#pragma warning restore 649

        [SerializeField]
        string m_ThemeName = String.Empty;
        public string Theme
        {
            get => m_ThemeName;
            set => m_ThemeName = value;
        }

        [SerializeField]
#pragma warning disable 649
        StickyNoteTextSize m_TextSize;
#pragma warning restore 649

        [SerializeField]
        string m_TextSizeName = String.Empty;
        public string TextSize
        {
            get => m_TextSizeName;
            set => m_TextSizeName = value;
        }

        [SerializeField]
        Rect m_Position;

        public Rect PositionAndSize
        {
            get => m_Position;
            set => m_Position = value;
        }

        public bool IsResizable => true;

        public Vector2 Position
        {
            get => PositionAndSize.position;
            set => PositionAndSize = new Rect(value, PositionAndSize.size);
        }

        public void Move(Vector2 delta)
        {
            Position += delta;
        }

        public ScriptableObject SerializableAsset => (ScriptableObject)AssetModel;
        public IGraphAssetModel AssetModel => VSGraphModel?.AssetModel;

        [SerializeField]
        GraphModel m_GraphModel;

        public IGraphModel VSGraphModel
        {
            get => m_GraphModel;
            set => m_GraphModel = (GraphModel)value;
        }

        public IGTFGraphModel GraphModel => VSGraphModel as IGTFGraphModel;

        public bool Destroyed { get; private set; }

        public void Destroy() => Destroyed = true;

        public string GetId()
        {
            return m_Id;
        }

        public void AssignNewGuid()
        {
            m_Id = Guid.NewGuid().ToString();
        }

        public StickyNoteModel Clone()
        {
            return new StickyNoteModel
            {
                Contents = Contents,
                Title = Title,
                Theme = Theme,
                TextSize = TextSize,
                PositionAndSize = PositionAndSize,
            };
        }

        public bool IsDeletable => true;

        public void OnBeforeSerialize()
        {
        }

        public void OnAfterDeserialize()
        {
            if (String.IsNullOrEmpty(m_ThemeName))
                m_ThemeName = m_Theme.ToString();

            if (String.IsNullOrEmpty(m_TextSizeName))
                m_TextSizeName = m_TextSize.ToString();
        }

        public bool IsCopiable => true;
        public bool IsRenamable => true;

        public void Rename(string newName)
        {
            Title = newName;
        }
    }
}
