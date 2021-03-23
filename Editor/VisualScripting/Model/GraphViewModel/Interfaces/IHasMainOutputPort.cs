using System;
using UnityEngine;

namespace UnityEditor.VisualScripting.GraphViewModel
{
    public interface IHasMainOutputPort : INodeModel
    {
        IPortModel OutputPort { get; }
    }
}
