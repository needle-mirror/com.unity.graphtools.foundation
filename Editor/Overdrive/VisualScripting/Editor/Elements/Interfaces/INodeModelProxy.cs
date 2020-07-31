using System;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting
{
    interface INodeModelProxy
    {
        ScriptableObject ScriptableObject();
        void SetModel(IGTFGraphElementModel model);
    }
}
