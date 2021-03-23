using System;
using UnityEngine;

namespace UnityEditor.VisualScripting.GraphViewModel
{
    public interface IHasInstancePort : INodeModel
    {
        IPortModel InstancePort { get; }
    }
}
