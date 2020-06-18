using System;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.GraphViewModel
{
    public interface IHasMainOutputPort : INodeModel
    {
        IPortModel OutputPort { get; }
    }
}
