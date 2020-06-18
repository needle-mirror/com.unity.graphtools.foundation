using System;
public class MathSubtractionOperator : MathOperator
{
    public void OnEnable()
    {
        name = "Subtract";
    }

    public override float Evaluate()
    {
        return (left != null ? left.Evaluate() : 0) - (right != null ? right.Evaluate() : 0);
    }
}
