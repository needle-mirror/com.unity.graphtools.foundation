using System;

namespace UnityEditor.GraphToolsFoundation.Overdrive.BasicModel
{
    /// <summary>
    /// Model for execution entry portals.
    /// </summary>
    [Serializable]
    public class ExecutionEdgePortalEntryModel : EdgePortalModel, IEdgePortalEntryModel
    {
        /// <inheritdoc />
        public IPortModel InputPort { get; private set; }

        /// <inheritdoc />
        protected override void OnDefineNode()
        {
            base.OnDefineNode();
            InputPort = this.AddExecutionInputPort("");
        }
    }
}
