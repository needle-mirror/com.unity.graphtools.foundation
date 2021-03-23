using System;
using UnityEngine;

namespace UnityEditor.VisualScripting.GraphViewModel
{
    public interface IHasMainInputPort : INodeModel
    {
        IPortModel InputPort { get; }
    }
}
