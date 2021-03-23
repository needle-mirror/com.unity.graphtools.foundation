using System;
using System.Linq;
using JetBrains.Annotations;
using UnityEditor.VisualScripting.GraphViewModel;

namespace UnityEditor.VisualScripting.Model
{
    [PublicAPI]
    [Serializable]
    public abstract class LoopNodeModel : NodeModel, IHasMainInputPort, IHasMainOutputPort
    {
        public override string Title => InsertLoopNodeTitle;
        public abstract string InsertLoopNodeTitle { get; }
        public virtual Type MatchingStackType => typeof(StackModel);

        public IPortModel InputPort { get; protected set; }
        public IPortModel OutputPort { get; private set; }

        public override string IconTypeString
        {
            get
            {
                if (OutputPort != null)
                {
                    var connectedPortNodeModel = OutputPort.ConnectionPortModels.FirstOrDefault()?.NodeModel as NodeModel;
                    if (connectedPortNodeModel != null)
                        return connectedPortNodeModel.IconTypeString;
                }

                return "typeLoop";
            }
        }

        protected override void OnDefineNode()
        {
            OutputPort = AddLoopOutputPort("", nameof(OutputPort));
        }
    }
}
