using System;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.GraphViewModel
{
    public enum StickyNoteTextSize
    {
        Small,
        Medium,
        Large,
        Huge
    }

    public enum StickyNoteColorTheme
    {
        Classic,
        Dark,
        Orange,
        Green,
        Blue,
        Red,
        Purple,
        Teal
    }

    [Serializable]
    [MovedFrom(false, "UnityEditor.VisualScripting.GraphViewModel", "Unity.GraphTools.Foundation.Overdrive.Editor")]
    public sealed class StickyNoteModel : IGTFStickyNoteModel, IGTFGraphElementModel, ISerializationCallbackReceiver, IGuidUpdate
    {
        [SerializeField]
        string m_Title;

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
#pragma warning disable 649 // Field is never assigned to.
        StickyNoteTextSize m_TextSize;
#pragma warning restore 649

        [SerializeField]
#pragma warning disable 649 // Field is never assigned to.
        string m_TextSizeName = String.Empty;
#pragma warning restore 649

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

        public IGTFGraphAssetModel AssetModel => GraphModel?.AssetModel;

        [SerializeField]
        GraphModel m_GraphModel;

        public IGTFGraphModel GraphModel
        {
            get => m_GraphModel;
            set => m_GraphModel = (GraphModel)value;
        }

        public bool Destroyed { get; private set; }

        public void Destroy() => Destroyed = true;

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

            if (String.IsNullOrEmpty(m_ThemeName))
                m_ThemeName = m_Theme.ToString();

            if (String.IsNullOrEmpty(m_TextSizeName))
                m_TextSizeName = m_TextSize.ToString();
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

        public bool IsCopiable => true;
        public bool IsRenamable => true;

        public void Rename(string newName)
        {
            Title = newName;
        }
    }
}
