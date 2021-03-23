using System;
using UnityEditor.VisualScripting.Editor.SmartSearch;
using UnityEngine;

namespace UnityEditor.VisualScripting.Editor
{
    public interface ICustomSearcherHandler
    {
        bool HandleCustomSearcher(Vector2 mousePosition, SearcherFilter filter = null);
    }
}
