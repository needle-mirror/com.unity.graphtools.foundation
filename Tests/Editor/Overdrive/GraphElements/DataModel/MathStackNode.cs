using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GraphElements
{
    public class MathStackNode : MathFunction
    {
        public enum Operation
        {
            Addition,
            Substraction,
            Multiplication
        }

        static private Dictionary<Operation, Func<float, float, float>> s_OperationFuncs
            = new Dictionary<Operation, Func<float, float, float>>
            {
            { Operation.Addition, (a, b) => a + b },
            { Operation.Substraction, (a, b) => a - b },
            { Operation.Multiplication, (a, b) => a * b }
            };

        [SerializeField]
        protected List<MathNodeID> m_NodeIDs = new List<MathNodeID>();

        [SerializeField]
        private Operation m_CurrentOperation = Operation.Addition;

        public Operation currentOperation
        {
            get
            {
                return m_CurrentOperation;
            }
            set
            {
                m_CurrentOperation = value;
            }
        }

        public int nodeCount { get { return m_NodeIDs.Count; } }

        public MathNode GetNode(int index)
        {
            return m_NodeIDs[index].Get(mathBook);
        }

        public void AddNode(MathNode node)
        {
            m_NodeIDs.Add(node.nodeID);
        }

        public void InsertNode(int index, MathNode node)
        {
            m_NodeIDs.Insert(index, node.nodeID);
        }

        public void InsertNodes(int index, IEnumerable<MathNode> nodes)
        {
            m_NodeIDs.InsertRange(index, nodes.Select(n => n.nodeID));
        }

        public void RemoveNode(MathNode node)
        {
            m_NodeIDs.Remove(node.nodeID);
        }

        public void RemoveNodes(IEnumerable<MathNode> nodes)
        {
            List<MathNodeID> toRemove = nodes.Select(n => n.nodeID).ToList();
            m_NodeIDs.RemoveAll(n => toRemove.Contains(n));
        }

        public void OnEnable()
        {
            if (m_ParameterIDs.Length == 0)
            {
                m_ParameterIDs = new MathNodeID[1];
            }

            if (m_ParameterNames.Length == 0)
            {
                m_ParameterNames = new string[] { "f" };
            }
        }

        public override void RemapReferences(Dictionary<string, string> oldIDNewIDMap)
        {
            base.RemapReferences(oldIDNewIDMap);

            for (int i = 0; i < m_NodeIDs.Count; i++)
            {
                MathNodeID newId = m_NodeIDs[i];

                RemapID(oldIDNewIDMap, ref newId);

                m_NodeIDs[i] = newId;
            }
        }

        public override float Evaluate()
        {
            float resultValue = 0;
            bool hasValidInput = false;

            if (GetParameter(0) != null)
            {
                resultValue = GetParameter(0).Evaluate();
                hasValidInput = true;
            }

            for (int i = 0; i < nodeCount; ++i)
            {
                MathNode node = GetNode(i);
                float currentValue = 0;

                if (node != null)
                {
                    currentValue = node.Evaluate();
                }

                if (i == 0 && !hasValidInput)
                {
                    resultValue = currentValue;
                }
                else
                {
                    resultValue = s_OperationFuncs[m_CurrentOperation](resultValue, currentValue);
                }
            }

            return resultValue;
        }
    }
}
