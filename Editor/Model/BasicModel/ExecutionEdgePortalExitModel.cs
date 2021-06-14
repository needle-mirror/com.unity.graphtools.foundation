using System;

namespace UnityEditor.GraphToolsFoundation.Overdrive.BasicModel
{
    /// <summary>
    /// Model for execution exit portals.
    /// </summary>
    [Serializable]
    public class ExecutionEdgePortalExitModel : EdgePortalModel, IEdgePortalExitModel
    {
        /// <inheritdoc />
        public IPortModel OutputPort => NodeModelDefaultImplementations.GetOutputPort(this);

        /// <inheritdoc />
        protected override void OnDefineNode()
        {
            base.OnDefineNode();
            this.AddExecutionOutputPort("");
        }
    }
}
