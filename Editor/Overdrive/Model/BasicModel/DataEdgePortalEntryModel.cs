using System;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEditor.GraphToolsFoundation.Overdrive.BasicModel
{
    [Serializable]
    //[MovedFrom(false, "UnityEditor.VisualScripting.GraphViewModel", "Unity.GraphTools.Foundation.Overdrive.Editor")]
    [MovedFrom("UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.GraphViewModel")]
    public class DataEdgePortalEntryModel : EdgePortalModel, IEdgePortalEntryModel, IHasMainInputPort
    {
        public IPortModel InputPort { get; private set; }

        public IPortModel MainInputPort => InputPort;

        protected override void OnDefineNode()
        {
            base.OnDefineNode();
            InputPort = this.AddDataInputPort("", TypeHandle.Unknown);
        }

        public DataEdgePortalEntryModel()
        {
            InternalInitCapabilities();
        }

        protected override void InitCapabilities()
        {
            base.InitCapabilities();
            InternalInitCapabilities();
        }

        void InternalInitCapabilities()
        {
            // Can't copy Data Entry portals as it makes no sense.
            this.SetCapability(Overdrive.Capabilities.Copiable, false);
        }
    }
}
