using System;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.MathBook
{
    [Serializable]
    public abstract class MathValueNode : MathNode
    {
        public abstract float value { get; }

        public override float Evaluate()
        {
            return value;
        }
    }
}
