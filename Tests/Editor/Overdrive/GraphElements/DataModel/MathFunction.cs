using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GraphElements
{
    public abstract class MathFunction : MathNode
    {
        [SerializeField]
        protected MathNodeID[] m_ParameterIDs = new MathNodeID[0];
        protected string[] m_ParameterNames = new string[0];

        public string[] parameterNames { get { return m_ParameterNames; } }

        public int parameterCount { get { return m_ParameterIDs.Length; } }

        public MathNode GetParameter(int index)
        {
            return m_ParameterIDs[index].Get(mathBook);
        }

        public void SetParameter(int index, MathNode node)
        {
            m_ParameterIDs[index].Set(node);
        }

        public override void ResetConnections()
        {
            for (int i = 0; i < m_ParameterIDs.Length; i++)
            {
                m_ParameterIDs[i] = MathNodeID.empty;
            }
        }

        public override void RemapReferences(Dictionary<string, string> oldIDNewIDMap)
        {
            base.RemapReferences(oldIDNewIDMap);

            for (int i = 0; i < m_ParameterIDs.Length; i++)
            {
                RemapID(oldIDNewIDMap, ref m_ParameterIDs[i]);
            }
        }
    }
}
