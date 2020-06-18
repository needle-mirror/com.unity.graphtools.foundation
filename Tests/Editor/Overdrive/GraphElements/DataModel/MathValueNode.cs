using UnityEngine;

public abstract class MathValueNode : MathNode
{
    public abstract float value { get; }

    public override float Evaluate()
    {
        return value;
    }
}
