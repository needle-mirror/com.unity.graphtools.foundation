namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.TestModels
{
    class IONodeModel : NodeModel
    {
        public IONodeModel()
        {
            this.SetCapability(Overdrive.Capabilities.Renamable, false);
        }

        public int ExeInputCount { get; set; }
        public int ExeOuputCount { get; set; }

        public int InputCount { get; set; }
        public int OuputCount { get; set; }

        protected override void OnDefineNode()
        {
            for (var i = 0; i < ExeInputCount; i++)
                AddExecutionInputPort<PortModel>("Exe In " + i);

            for (var i = 0; i < ExeOuputCount; i++)
                AddExecutionOutputPort<PortModel>("Exe Out " + i);

            for (var i = 0; i < InputCount; i++)
                AddDataInputPort<PortModel>("In " + i, TypeHandle.Unknown);

            for (var i = 0; i < OuputCount; i++)
                AddDataOutputPort<PortModel>("Out " + i, TypeHandle.Unknown);
        }
    }
}
