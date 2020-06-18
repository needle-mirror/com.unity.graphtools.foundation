using System;
using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.GraphViewModel;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.SmartSearch
{
    public interface ISearcherItemData
    {
    }

    public enum CommonSearcherTags
    {
        StickyNote
    }

    public interface ITaggedSearcherTag : ISearcherItemData
    {
        CommonSearcherTags Tag { get; }
    }

    public readonly struct TagSearcherItemData : ITaggedSearcherTag
    {
        public CommonSearcherTags Tag { get; }

        public TagSearcherItemData(CommonSearcherTags tag)
        {
            Tag = tag;
        }
    }

    public readonly struct TypeSearcherItemData : ISearcherItemData
    {
        public enum CommonSearcherTags
        {
            Type,
            Constant
        }

        public bool IsConstant => Tag == CommonSearcherTags.Constant;
        public TypeHandle Type { get; }

        public CommonSearcherTags Tag { get; }

        public TypeSearcherItemData(TypeHandle type, CommonSearcherTags tag = CommonSearcherTags.Type)
        {
            Type = type;
            Tag = tag;
        }

        public static TypeSearcherItemData Constant(TypeHandle type)
        {
            return new TypeSearcherItemData(type, CommonSearcherTags.Constant);
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

    public readonly struct GraphAssetSearcherItemData : ISearcherItemData
    {
        public IGraphAssetModel GraphAssetModel { get; }

        public GraphAssetSearcherItemData(IGraphAssetModel graphAssetModel)
        {
            GraphAssetModel = graphAssetModel;
        }
    }
}
