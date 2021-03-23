using System;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.MathBook
{
    [Serializable]
    public class PIConstant : MathNode
    {
        public PIConstant()
        {
            Title = "PI";
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
