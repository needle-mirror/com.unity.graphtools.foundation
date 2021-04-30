using System;
using System.Linq;
using UnityEngine;


namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.MathBook
{
    [Serializable]
    public abstract class MathFunction : MathNode
    {
        [SerializeField]
        protected string[] m_ParameterNames = new string[0];

        public string[] parameterNames { get { return m_ParameterNames; } }

        public int parameterCount { get { return m_ParameterNames.Length; } }

        public float GetParameterValue(int index)
        {
            IPortModel port = this.GetInputPorts().Skip(index).FirstOrDefault();

            if (port == null)
            {
                Debug.LogError("Access to unavailable port " + index);
                return 0;
            }

            return GetValue(port);
        }

        public override void ResetConnections()
        {
        }

        public IPortModel DataOut0 { get; private set; }

        protected override void OnDefineNode()
        {
            foreach (var name in m_ParameterNames)
            {
                this.AddDataInputPort<float>(name);
            }
            DataOut0 = this.AddDataOutputPort<float>("out");
        }
    }
}
