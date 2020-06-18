using UnityEngine;

public class MathAdditionOperator : MathOperator
{
    public void OnEnable()
    {
        name = "Add";
    }

    public override float Evaluate()
    {
        return (left != null ? left.Evaluate() : 0) + (right != null ? right.Evaluate() : 0);
    }
}
