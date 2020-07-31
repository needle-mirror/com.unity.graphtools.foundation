using System;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.GraphViewModel
{
    public interface IHasMainInputPort : IGTFNodeModel
    {
        IGTFPortModel MainInputPort { get; }
    }
}
