using System;
using UnityEngine;

namespace UnityEditor.VisualScripting.GraphViewModel
{
    [Serializable]
    public sealed class StickyNoteModel : IStickyNoteModel
    {
        [SerializeField]
        string m_Title;

        [SerializeField]
        string m_Id = Guid.NewGuid().ToString();

        public StickyNoteModel()
        {
            Title = string.Empty;
            Contents = string.Empty;
            Theme = StickyNoteColorTheme.Classic;
            TextSize = StickyNoteTextSize.Small;
        }

        public string Title
        {
            get => m_Title;
            private set { if (value != null && m_Title != value) m_Title = value; }
        }

        [SerializeField]
        string m_Contents;
        public string Contents
        {
            get => m_Contents;
            private set { if (value != null && m_Contents != value) m_Contents = value; }
        }

        [SerializeField]
        StickyNoteColorTheme m_Theme;
        public StickyNoteColorTheme Theme
        {
            get => m_Theme;
            private set => m_Theme = value;
        }

        [SerializeField]
        StickyNoteTextSize m_TextSize;
        public StickyNoteTextSize TextSize
        {
            get => m_TextSize;
            private set => m_TextSize = value;
        }

        [SerializeField]
        Rect m_Position;

        public Rect Position
        {
            get => m_Position;
            set => m_Position = value;
        }

        public void Move(Rect newPosition)
        {
            Position = newPosition;
        }

        public void UpdateBasicSettings(string newTitle, string newContents)
        {
            Title = newTitle;
            Contents = newContents;
        }

        public void UpdateTheme(StickyNoteColorTheme newTheme)
        {
            Theme = newTheme;
        }

        public void UpdateTextSize(StickyNoteTextSize newTextSize)
        {
            TextSize = newTextSize;
        }

        // Capabilities
#if UNITY_2020_1_OR_NEWER
        public CapabilityFlags Capabilities => CapabilityFlags.Selectable | CapabilityFlags.Deletable | CapabilityFlags.Movable | CapabilityFlags.Copiable;
#else
        public CapabilityFlags Capabilities => CapabilityFlags.Selectable | CapabilityFlags.Deletable | CapabilityFlags.Movable;
#endif

        public ScriptableObject SerializableAsset => (ScriptableObject)AssetModel;
        public IGraphAssetModel AssetModel => GraphModel?.AssetModel;

        [SerializeField]
        GraphModel m_GraphModel;

        public IGraphModel GraphModel
        {
            get => m_GraphModel;
            set => m_GraphModel = (GraphModel)value;
        }

        public bool Destroyed { get; private set; }

        public void Destroy() => Destroyed = true;

        public string GetId()
        {
            return m_Id;
        }

        public StickyNoteModel Clone()
        {
            return new StickyNoteModel
            {
                Contents = Contents,
                Title = Title,
                Theme = Theme,
                Position = Position,
            };
        }
    }
}
