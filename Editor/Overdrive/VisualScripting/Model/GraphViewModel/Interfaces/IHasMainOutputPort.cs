using System;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.GraphViewModel
{
    public interface IHasMainOutputPort : IGTFNodeModel
    {
        IGTFPortModel MainOutputPort { get; }
    }
}
