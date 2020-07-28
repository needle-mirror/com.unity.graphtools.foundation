using System;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEditor.GraphToolsFoundation.Overdrive.BasicModel
{
    [Serializable]
    //[MovedFrom(false, "UnityEditor.VisualScripting.GraphViewModel", "Unity.GraphTools.Foundation.Overdrive.Editor")]
    [MovedFrom("UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.GraphViewModel")]
    public class ExecutionEdgePortalEntryModel : EdgePortalModel, IGTFEdgePortalEntryModel, IHasMainExecutionInputPort
    {
        public IGTFPortModel InputPort { get; private set; }

        public IGTFPortModel ExecutionInputPort => InputPort;

        protected override void OnDefineNode()
        {
            base.OnDefineNode();
            InputPort = AddExecutionInputPort("");
        }
    }
}
