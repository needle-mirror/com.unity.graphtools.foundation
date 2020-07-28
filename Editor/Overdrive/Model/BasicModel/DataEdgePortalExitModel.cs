using System;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEditor.GraphToolsFoundation.Overdrive.BasicModel
{
    [Serializable]
    //[MovedFrom(false, "UnityEditor.VisualScripting.GraphViewModel", "Unity.GraphTools.Foundation.Overdrive.Editor")]
    [MovedFrom("UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.GraphViewModel")]
    public class DataEdgePortalExitModel : EdgePortalModel, IGTFEdgePortalExitModel, IHasMainOutputPort
    {
        public IGTFPortModel OutputPort => NodeModelDefaultImplementations.GetOutputPort(this);

        public IGTFPortModel MainOutputPort => OutputPort;

        protected override void OnDefineNode()
        {
            base.OnDefineNode();
            AddDataOutputPort("", TypeHandle.Unknown);
        }

        public override bool CanCreateOppositePortal()
        {
            return !GraphModel.FindReferencesInGraph<IGTFEdgePortalEntryModel>(DeclarationModel).Any();
        }
    }
}
