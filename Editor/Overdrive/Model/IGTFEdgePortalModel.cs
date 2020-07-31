using System;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Model
{
    public interface IGTFEdgePortalModel : IGTFNodeModel, IHasTitle, IHasDeclarationModel
    {
        int EvaluationOrder { get; }
        bool CanCreateOppositePortal();
    }

    public interface IGTFEdgePortalEntryModel : IGTFEdgePortalModel, IHasSingleInputPort
    {
    }

    public interface IGTFEdgePortalExitModel : IGTFEdgePortalModel, IHasSingleOutputPort
    {
    }
}
