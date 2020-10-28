using System.Collections.Generic;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GraphElements
{
    public class MathResult : MathNode
    {
        public MathNodeID m_RootID = MathNodeID.empty;

        public MathNode root
        {
            get
            {
                return m_RootID.Get(mathBook);
            }
            set
            {
                m_RootID.Set(value);
            }
        }

        public void OnEnable()
        {
            name = "MathResult";
        }

        public override void ResetConnections()
        {
            root = null;
        }

        public override float Evaluate()
        {
            if (root == null)
                return 0;

            return root.Evaluate();
        }

        public override void RemapReferences(Dictionary<string, string> oldIDNewIDMap)
        {
            base.RemapReferences(oldIDNewIDMap);
            RemapID(oldIDNewIDMap, ref m_RootID);
        }
    }
}
