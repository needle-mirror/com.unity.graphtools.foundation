using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GTFO.UIFromModelTests
{
    class NodeModel : IGTFNodeModel, IHasPorts, IHasTitle, ICollapsible, IRenamable
    {
        public IGTFGraphModel GraphModel { get; set; }
        public Vector2 Position { get; set; }

        public void Move(Vector2 delta)
        {
            Position += delta;
        }

        public bool IsDeletable => true;
        public bool IsDroppable => true;
        public bool IsCopiable => true;
        public IEnumerable<IGTFPortModel> Ports => Enumerable.Empty<IGTFPortModel>();
        public string Title { get; set; }
        public string DisplayTitle => Title;
        public bool Collapsed { get; set; }
        public bool IsRenamable => true;
        public void Rename(string newName)
        {
            Title = newName;
        }
    }
}
