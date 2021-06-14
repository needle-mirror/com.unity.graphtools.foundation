using System;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.MathBook
{
    [Serializable]
    public abstract class MathNode : NodeModel
    {
        public float GetValue(IPortModel port)
        {
            return port.GetValue();
        }

        public abstract float Evaluate();

        public abstract void ResetConnections();
    }
}
