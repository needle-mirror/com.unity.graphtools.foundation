using System;
using System.Linq;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEditor.GraphToolsFoundation.Overdrive.BasicModel
{
    [Serializable]
    //[MovedFrom(false, "UnityEditor.VisualScripting.GraphViewModel", "Unity.GraphTools.Foundation.Overdrive.Editor")]
    [MovedFrom("UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.GraphViewModel")]
    public class DataEdgePortalExitModel : EdgePortalModel, IEdgePortalExitModel, IHasMainOutputPort
    {
        public IPortModel OutputPort => NodeModelDefaultImplementations.GetOutputPort(this);

        public IPortModel MainOutputPort => OutputPort;

        protected override void OnDefineNode()
        {
            base.OnDefineNode();
            this.AddDataOutputPort("", TypeHandle.Unknown);
        }

        public override bool CanCreateOppositePortal()
        {
            return !GraphModel.FindReferencesInGraph<IEdgePortalEntryModel>(DeclarationModel).Any();
        }
    }
}
