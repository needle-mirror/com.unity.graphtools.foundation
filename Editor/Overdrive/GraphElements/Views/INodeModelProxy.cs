using System;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    interface INodeModelProxy
    {
        ScriptableObject ScriptableObject();
        void SetModel(IGraphElementModel model);
    }
}
