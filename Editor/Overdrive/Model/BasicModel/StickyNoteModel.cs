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
    public class StickyNoteModel : IStickyNoteModel, ISerializationCallbackReceiver, IGuidUpdate
    {
        [SerializeField]
        string m_Title;

        [SerializeField]
        string m_Contents;

        [SerializeField, Obsolete]
#pragma warning disable 649 // Field is never assigned to.
        StickyNoteColorTheme m_Theme;
#pragma warning restore 649

        [SerializeField]
        string m_ThemeName = String.Empty;

        [SerializeField, Obsolete]
#pragma warning disable 649 // Field is never assigned to.
        StickyNoteTextSize m_TextSize;
#pragma warning restore 649

        [SerializeField]
        string m_TextSizeName = String.Empty;

        [SerializeField]
        Rect m_Position;

        [SerializeField, Obsolete]
        GraphModel m_GraphModel;

        [SerializeField, HideInInspector]
        GraphAssetModel m_GraphAssetModel;

        [SerializeField]
        SerializableGUID m_Guid;

        [SerializeField, Obsolete]
        string m_Id = "";

        [SerializeField]
        List<string> m_SerializedCapabilities;

        List<Capabilities> m_Capabilities;

        public IGraphAssetModel AssetModel
        {
            get => m_GraphAssetModel;
            set => m_GraphAssetModel = (GraphAssetModel)value;
        }

        public virtual IGraphModel GraphModel => m_GraphAssetModel.GraphModel;

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

        public IReadOnlyList<Capabilities> Capabilities => m_Capabilities;

        public void OnBeforeSerialize()
        {
            m_SerializedCapabilities = m_Capabilities?.Select(c => c.Name).ToList() ?? new List<string>();
        }

        public void OnAfterDeserialize()
        {
#pragma warning disable 612
            if (m_Guid.GUID.Empty())
            {
                if (!String.IsNullOrEmpty(m_Id))
                {
                    (GraphModel as GraphModel)?.AddGuidToUpdate(this, m_Id);
                }
            }

            if (String.IsNullOrEmpty(m_ThemeName))
                m_ThemeName = m_Theme.ToString();

            if (String.IsNullOrEmpty(m_TextSizeName))
                m_TextSizeName = m_TextSize.ToString();

            m_GraphAssetModel = (GraphAssetModel)m_GraphModel?.AssetModel;
            m_GraphModel = null;
#pragma warning restore 612

            if (!m_SerializedCapabilities.Any())
                // If we're reloading an older node
                InitCapabilities();
            else
                m_Capabilities = m_SerializedCapabilities.Select(Overdrive.Capabilities.Get).ToList();
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
