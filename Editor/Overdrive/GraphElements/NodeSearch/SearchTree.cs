using System;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEditor.GraphToolsFoundation.Overdrive.GraphElements
{
    [Serializable]
    [MovedFrom(false, "Unity.GraphElements", "Unity.GraphTools.Foundation.Overdrive.Editor")]
    public class SearchTreeEntry : IComparable<SearchTreeEntry>
    {
        public int level;
        public GUIContent content;

        public object userData;

        public SearchTreeEntry(GUIContent content)
        {
            this.content = content;
        }

        public string name
        {
            get { return content.text; }
        }

        public int CompareTo(SearchTreeEntry o)
        {
            return name.CompareTo(o.name);
        }
    }

    [Serializable]
    [MovedFrom(false, "Unity.GraphElements", "Unity.GraphTools.Foundation.Overdrive.Editor")]
    public class SearchTreeGroupEntry : SearchTreeEntry
    {
        internal int selectedIndex;
        internal Vector2 scroll;

        public SearchTreeGroupEntry(GUIContent content, int level = 0)
            : base(content)
        {
            this.content = content;
            this.level = level;
        }
    }
}
