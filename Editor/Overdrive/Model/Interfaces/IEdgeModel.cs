namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public interface IEdgeModel : IGraphElementModel
    {
        IPortModel FromPort { get; set; }
        IPortModel ToPort { get; set; }
        void SetPorts(IPortModel toPortModel, IPortModel fromPortModel);
        void ResetPorts();

        string FromPortId { get; }
        string ToPortId { get; }
        GUID ToNodeGuid { get; }
        GUID FromNodeGuid { get; }

        string EdgeLabel { get; set; }
    }
}
