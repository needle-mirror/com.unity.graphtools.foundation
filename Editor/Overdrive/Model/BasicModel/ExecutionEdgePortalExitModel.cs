using System;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEditor.GraphToolsFoundation.Overdrive.BasicModel
{
    [Serializable]
    //[MovedFrom(false, "UnityEditor.VisualScripting.GraphViewModel", "Unity.GraphTools.Foundation.Overdrive.Editor")]
    [MovedFrom("UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.GraphViewModel")]
    public class ExecutionEdgePortalExitModel : EdgePortalModel, IGTFEdgePortalExitModel, IHasMainExecutionOutputPort
    {
        public IGTFPortModel OutputPort => NodeModelDefaultImplementations.GetOutputPort(this);

        public IGTFPortModel ExecutionOutputPort => OutputPort;

        protected override void OnDefineNode()
        {
            base.OnDefineNode();
            AddExecutionOutputPort("");
        }
    }
}
