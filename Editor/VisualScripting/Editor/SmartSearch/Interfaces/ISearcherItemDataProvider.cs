using System;

namespace UnityEditor.VisualScripting.Editor.SmartSearch
{
    public interface ISearcherItemDataProvider
    {
        ISearcherItemData Data { get; }
    }
}
