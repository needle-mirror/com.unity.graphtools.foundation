using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEditor.GraphToolsFoundation.Overdrive.BasicModel
{
    /// <summary>
    /// A model that represents a sticky note in a graph.
    /// </summary>
    [Serializable]
    //[MovedFrom(false, "UnityEditor.VisualScripting.GraphViewModel", "Unity.GraphTools.Foundation.Overdrive.Editor")]
    [MovedFrom("UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.GraphViewModel")]
    public class StickyNoteModel : GraphElementModel, IStickyNoteModel
    {
        [SerializeField]
        string m_Title;

        [SerializeField]
        string m_Contents;

        [SerializeField]
        string m_ThemeName = String.Empty;

        [SerializeField]
        string m_TextSizeName = String.Empty;

        [SerializeField]
        Rect m_Position;

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

        public string Title
        {
            get => m_Title;
            set { if (value != null && m_Title != value) m_Title = value; }
        }

        public string DisplayTitle => Title;

        public string Contents
        {
            get => m_Contents;
            set { if (value != null && m_Contents != value) m_Contents = value; }
        }

        public string Theme
        {
            get => m_ThemeName;
            set => m_ThemeName = value;
        }

        public string TextSize
        {
            get => m_TextSizeName;
            set => m_TextSizeName = value;
        }

        public bool Destroyed { get; private set; }

        public StickyNoteModel()
        {
            InternalInitCapabilities();
            Title = string.Empty;
            Contents = string.Empty;
            Theme = StickyNoteColorTheme.Classic.ToString();
            TextSize = StickyNoteTextSize.Small.ToString();
            PositionAndSize = Rect.zero;
        }

        public void Destroy() => Destroyed = true;

        public void Move(Vector2 delta)
        {
            if (!this.IsMovable())
                return;

            Position += delta;
        }

        public void Rename(string newName)
        {
            if (!this.IsRenamable())
                return;

            Title = newName;
        }

        /// <inheritdoc />
        protected override void InitCapabilities()
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
                Overdrive.Capabilities.Resizable
            };
        }
    }

    enum StickyNoteTextSize
    {
        Small,
        Medium,
        Large,
        Huge
    }

    enum StickyNoteColorTheme
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
}
