using System;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.MathBook
{
    [Serializable]
    public class PIConstant : MathNode
    {
        public override string Title
        {
            get => "π";
            set { }
        }

        public override float Evaluate()
        {
            return Mathf.PI;
        }

        public override void ResetConnections()
        {
        }

        protected override void OnDefineNode()
        {
            this.AddDataOutputPort<float>("");
        }
    }
}
