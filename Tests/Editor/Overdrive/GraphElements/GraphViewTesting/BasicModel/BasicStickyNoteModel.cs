using System;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GraphElements.Utilities
{
    public class BasicStickyNoteModel : IGTFStickyNoteModel
    {
        public string Title { get; set; }
        public string DisplayTitle => Title;
        public string Contents { get; set; }
        public Rect PositionAndSize { get; set;  }
        public bool IsResizable => true;
        public string Theme { get; set; }
        public string TextSize { get; set; }
        public Vector2 Position
        {
            get => PositionAndSize.position;
            set => PositionAndSize = new Rect(value, PositionAndSize.size);
        }
        public IGTFGraphModel GraphModel { get; set; }

        GUID m_GUID = GUID.Generate();
        public GUID Guid
        {
            get => m_GUID;
            set => m_GUID = value;
        }

        public IGTFGraphAssetModel AssetModel
        {
            get => GraphModel.AssetModel;
            set => GraphModel.AssetModel = value;
        }

        public void AssignNewGuid()
        {
            m_GUID = GUID.Generate();
        }

        public bool IsDeletable => true;
        public bool IsCopiable => true;
        public void Move(Vector2 delta)
        {
            Position += delta;
        }

        public bool IsRenamable => true;

        public void Rename(string newName)
        {
            Title = newName;
        }

        public bool Destroyed { get; private set; }
        public void Destroy() => Destroyed = true;
    }
}
