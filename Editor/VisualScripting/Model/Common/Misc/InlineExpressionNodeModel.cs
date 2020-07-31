using System;
using System.Linq;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEngine;

namespace UnityEditor.VisualScripting.Model
{
    [Serializable]
    public class InlineExpressionNodeModel : NodeModel, IRenamableModel
    {
        public override string Title => Expression;

        public override CapabilityFlags Capabilities => base.Capabilities | CapabilityFlags.Renamable;

        [SerializeField]
        string m_Expression;

        public string Expression
        {
            get => m_Expression;
            set => m_Expression = value;
        }

        public IPortModel MainOutputPort { get; private set; }

        protected override void OnDefineNode()
        {
            Parse();
            MainOutputPort = AddDataOutputPort<float>(null, nameof(MainOutputPort));
        }

        void Parse()
        {
        }

        public void Rename(string newName)
        {
            Expression = newName;
            ((VSGraphModel)GraphModel).LastChanges.ChangedElements.Add(this);
            DefineNode();
        }
    }
}
