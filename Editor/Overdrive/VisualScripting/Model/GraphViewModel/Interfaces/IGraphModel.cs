using System;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.GraphViewModel
{
    public interface IGraphModel : IGTFGraphModel
    {
        void ResetChanges();
        void CleanUp();
    }
}