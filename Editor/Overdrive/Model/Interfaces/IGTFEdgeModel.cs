using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Model
{
    public interface IGTFEdgeModel : IGTFGraphElementModel, ISelectable, IDeletable, ICopiable
    {
        IGTFPortModel FromPort { get; set; }
        IGTFPortModel ToPort { get; set; }
        void SetPorts(IGTFPortModel toPortModel, IGTFPortModel fromPortModel);
        void ResetPorts();

        string FromPortId { get; }
        string ToPortId { get; }
        GUID ToNodeGuid { get; }
        GUID FromNodeGuid { get; }

        string EdgeLabel { get; set; }
    }
}
