using System;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public interface ISearcherItemData
    {
    }

    public enum CommonSearcherTags
    {
        StickyNote
    }

    public readonly struct TagSearcherItemData : ISearcherItemData
    {
        public CommonSearcherTags Tag { get; }

        public TagSearcherItemData(CommonSearcherTags tag)
        {
            Tag = tag;
        }
    }

    public readonly struct TypeSearcherItemData : ISearcherItemData
    {
        public TypeHandle Type { get; }

        public TypeSearcherItemData(TypeHandle type)
        {
            Type = type;
        }
    }

    public readonly struct NodeSearcherItemData : ISearcherItemData
    {
        public Type Type { get; }

        public NodeSearcherItemData(Type type)
        {
            Type = type;
        }
    }
}
