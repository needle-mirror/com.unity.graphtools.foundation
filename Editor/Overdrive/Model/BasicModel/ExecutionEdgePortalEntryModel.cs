using System;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEditor.GraphToolsFoundation.Overdrive.BasicModel
{
    [Serializable]
    //[MovedFrom(false, "UnityEditor.VisualScripting.GraphViewModel", "Unity.GraphTools.Foundation.Overdrive.Editor")]
    //[MovedFrom("UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.GraphViewModel")]
    [MovedFrom(false, sourceAssembly: "Unity.GraphTools.Foundation.Overdrive.Editor")]
    public class ExecutionEdgePortalEntryModel : EdgePortalModel, IEdgePortalEntryModel, IHasMainExecutionInputPort
    {
        public IPortModel InputPort { get; private set; }

        public IPortModel ExecutionInputPort => InputPort;

        protected override void OnDefineNode()
        {
            base.OnDefineNode();
            InputPort = this.AddExecutionInputPort("");
        }
    }
}
