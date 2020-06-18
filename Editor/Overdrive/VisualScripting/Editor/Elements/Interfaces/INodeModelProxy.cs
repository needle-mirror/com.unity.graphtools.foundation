using System;
using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.GraphViewModel;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting
{
    internal interface INodeModelProxy
    {
        ScriptableObject ScriptableObject();
        void SetModel(IGraphElementModel model);
    }
}
