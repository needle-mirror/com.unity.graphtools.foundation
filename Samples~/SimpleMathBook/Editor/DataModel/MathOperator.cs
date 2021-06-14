using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.MathBook
{
    [Serializable]
    public abstract class MathOperator : MathNode
    {
        [SerializeField, HideInInspector]
        int m_InputPortCount = 2;

        public List<float> Values => this.GetInputPorts().Select(portModel => portModel == null ? 0 : GetValue(portModel)).ToList();

        public override void ResetConnections()
        {
        }

        public int InputPortCount
        {
            get => m_InputPortCount;
            set => m_InputPortCount = Math.Max(2, value);
        }

        public IPortModel DataOut { get; private set; }

        protected override void OnDefineNode()
        {
            base.OnDefineNode();

            DataOut = this.AddDataOutputPort<float>("Output");

            AddInputPorts();
        }

        protected abstract void AddInputPorts();
    }
}
