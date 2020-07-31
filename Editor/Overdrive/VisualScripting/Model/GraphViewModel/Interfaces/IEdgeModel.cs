using System;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.GraphViewModel
{
    public interface IEdgeModel : IGTFEdgeModel
    {
        string FromPortId { get; }
        string ToPortId { get; }
        GUID ToNodeGuid { get; }
        GUID FromNodeGuid { get; }
    }
}
