using System;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEditor.GraphToolsFoundation.Overdrive.BasicModel
{
    [Serializable]
    //[MovedFrom(false, "UnityEditor.VisualScripting.GraphViewModel", "Unity.GraphTools.Foundation.Overdrive.Editor")]
    [MovedFrom("UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.GraphViewModel")]
    public class DataEdgePortalEntryModel : EdgePortalModel, IGTFEdgePortalEntryModel, IHasMainInputPort
    {
        public IGTFPortModel InputPort { get; private set; }

        // Can't copy Data Entry portals as it makes no sense.
        public override bool IsCopiable => false;

        public IGTFPortModel MainInputPort => InputPort;

        protected override void OnDefineNode()
        {
            base.OnDefineNode();
            InputPort = AddDataInputPort("", TypeHandle.Unknown);
        }
    }
}
