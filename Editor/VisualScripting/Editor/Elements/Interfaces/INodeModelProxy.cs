using System;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEngine;

namespace UnityEditor.VisualScripting.Editor
{
    internal interface INodeModelProxy
    {
        ScriptableObject ScriptableObject();
        void SetModel(IGraphElementModel model);
    }
}
