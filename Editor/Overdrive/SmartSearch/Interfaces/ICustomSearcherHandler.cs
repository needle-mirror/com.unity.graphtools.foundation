using System;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public interface ICustomSearcherHandler
    {
        bool HandleCustomSearcher(Vector2 mousePosition, SearcherFilter filter = null);
    }
}
