using System;
using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.SmartSearch;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting
{
    public interface ICustomSearcherHandler
    {
        bool HandleCustomSearcher(Vector2 mousePosition, SearcherFilter filter = null);
    }
}
