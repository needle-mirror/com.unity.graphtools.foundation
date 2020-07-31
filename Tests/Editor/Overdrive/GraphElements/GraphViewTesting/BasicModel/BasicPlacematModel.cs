using System;
using System.Collections.Generic;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GraphElements.Utilities
{
    public class BasicPlacematModel : IGTFPlacematModel
    {
        public IGTFGraphModel GraphModel { get; set; }

        GUID m_GUID = GUID.Generate();
        public GUID Guid => m_GUID;
        public IGTFGraphAssetModel AssetModel => GraphModel.AssetModel;

        public void AssignNewGuid()
        {
            m_GUID = GUID.Generate();
        }

        public string Title { get; set; }
        public string DisplayTitle => Title;
        public bool IsDeletable => true;
        public bool Collapsed { get; set; }
        public Rect PositionAndSize { get; set; }
        public bool IsResizable => !Collapsed;
        public bool IsRenamable => true;
        public void Rename(string newName)
        {
            Title = newName;
        }

        public Color Color { get; set; }
        public int ZOrder { get; set; }
        public IEnumerable<IGTFGraphElementModel> HiddenElements { get; set; }

        public BasicPlacematModel(string title)
        {
            Title = title;
        }

        public Vector2 Position
        {
            get => PositionAndSize.position;
            set => PositionAndSize = new Rect(value, PositionAndSize.size);
        }

        public void Move(Vector2 delta)
        {
            Position += delta;
        }

        public bool IsCopiable => true;
        public bool Destroyed { get; private set; }
        public void Destroy() => Destroyed = true;
    }
}
