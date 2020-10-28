using System;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEditor.GraphToolsFoundation.Overdrive.BasicModel
{
    [Serializable]
    //[MovedFrom(false, "UnityEditor.VisualScripting.GraphViewModel", "Unity.GraphTools.Foundation.Overdrive.Editor")]
    [MovedFrom("UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.GraphViewModel")]
    public class ExecutionEdgePortalExitModel : EdgePortalModel, IEdgePortalExitModel, IHasMainExecutionOutputPort
    {
        public IPortModel OutputPort => NodeModelDefaultImplementations.GetOutputPort(this);

        public IPortModel ExecutionOutputPort => OutputPort;

        protected override void OnDefineNode()
        {
            base.OnDefineNode();
            AddExecutionOutputPort("");
        }
    }
}
