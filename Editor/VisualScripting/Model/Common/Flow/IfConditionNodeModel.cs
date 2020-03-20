using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.VisualScripting.GraphViewModel;

namespace UnityEditor.VisualScripting.Model
{
    [BranchedNode]
    [Serializable]
    public class IfConditionNodeModel : NodeModel
    {
        public override string Title => "If";
        public override bool IsCondition => true;

        public override string IconTypeString => "typeIfCondition";

        public IPortModel IfPort { get; private set; }
        public IPortModel ThenPort { get; private set; }
        public IPortModel ElsePort { get; private set; }

        List<ConditionWithActionPorts> m_ConditionsActionsPorts;
        public struct ConditionWithActionPorts
        {
            public IPortModel Condition, Action;

            public ConditionWithActionPorts(IPortModel conditionPort, IPortModel actionPort)
            {
                Condition = conditionPort;
                Action = actionPort;
            }
        }

        public IEnumerable<ConditionWithActionPorts> ConditionsWithActionsPorts => m_ConditionsActionsPorts;

        protected override void OnDefineNode()
        {
            IfPort = AddDataInputPort<bool>("Condition");
            ThenPort = AddExecutionOutputPort("Then");
            ElsePort = AddExecutionOutputPort("Else");

            // Right now we don't use more than If/Then/Else
            // but the translator can already operate on several levels of if/elseif/else
            m_ConditionsActionsPorts = new List<ConditionWithActionPorts>
            {
                new ConditionWithActionPorts(IfPort, ThenPort),
                new ConditionWithActionPorts(null, ElsePort)
            };
        }
    }
}
