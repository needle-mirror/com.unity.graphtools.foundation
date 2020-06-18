using System;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.GraphViewModel
{
    public interface IHasMainInputPort : INodeModel
    {
        IPortModel InputPort { get; }
    }
}
