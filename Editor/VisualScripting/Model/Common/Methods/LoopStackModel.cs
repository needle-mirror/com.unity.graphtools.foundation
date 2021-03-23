using System;
using System.Collections.Generic;
using UnityEditor.VisualScripting.GraphViewModel;

namespace UnityEditor.VisualScripting.Model
{
    [Serializable]
    public abstract class LoopStackModel : FunctionModel, IHasMainInputPort
    {
        public override bool HasReturnType => false;

        public override bool IsInstanceMethod => true;

        public override bool IsEntryPoint => false;

        public override CapabilityFlags Capabilities => base.Capabilities & ~CapabilityFlags.Renamable;

        public override string IconTypeString => "typeLoop";

        public abstract Type MatchingStackedNodeType { get; }

        public enum TitleComponentType
        {
            String,
            Token
        }

        public enum TitleComponentIcon
        {
            None,
            Collection,
            Condition,
            Count,
            Index,
            Item
        }

        public struct TitleComponent
        {
            public TitleComponentType titleComponentType;
            public object titleObject;
            public TitleComponentIcon titleComponentIcon;
        }

        public IPortModel InputPort { get; private set; }
        public abstract List<TitleComponent> BuildTitle();

        protected override void OnDefineNode()
        {
            InputPort = AddExecutionInputPort(null);
            OutputPort = AddExecutionOutputPort(null);
        }

        public LoopNodeModel CreateLoopNode(StackBaseModel hostStack, int index,
            SpawnFlags spawnFlags = SpawnFlags.Default, Action<NodeModel> setup = null, GUID? guid = null)
        {
            return (LoopNodeModel)hostStack.CreateStackedNode(
                MatchingStackedNodeType, Title, index, spawnFlags, setup, guid);
        }
    }
}
