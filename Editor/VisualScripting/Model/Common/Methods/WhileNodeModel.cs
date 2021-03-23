using System;
using UnityEditor.VisualScripting.GraphViewModel;

namespace UnityEditor.VisualScripting.Model
{
    [Serializable]
    public class WhileNodeModel : LoopNodeModel
    {
        public override bool IsInsertLoop => true;
        public override LoopConnectionType LoopConnectionType => LoopConnectionType.LoopStack;

        public override string InsertLoopNodeTitle => "While Loop";
        public override Type MatchingStackType => typeof(WhileHeaderModel);

        protected override void OnDefineNode()
        {
            base.OnDefineNode();

            InputPort = AddDataInputPort<bool>(WhileHeaderModel.DefaultConditionName);
        }
    }
}
