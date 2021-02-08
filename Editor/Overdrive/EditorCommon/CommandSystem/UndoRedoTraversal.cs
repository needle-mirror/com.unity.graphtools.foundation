namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    class UndoRedoTraversal : GraphTraversal
    {
        protected override void VisitEdge(IEdgeModel edgeModel)
        {
            base.VisitEdge(edgeModel);
            edgeModel.ResetPorts();
        }
    }
}
