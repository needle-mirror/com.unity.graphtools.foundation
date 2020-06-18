using UnityEngine;

public class RoundFunction : MathFunction
{
    void OnEnable()
    {
        name = "Round";
        if (m_ParameterIDs.Length == 0)
        {
            m_ParameterIDs = new MathNodeID[1];
        }

        if (m_ParameterNames.Length == 0)
        {
            m_ParameterNames = new string[] { "f" };
        }
    }

    public override float Evaluate()
    {
        float input = 0.0f;
        if (GetParameter(0) != null)
        {
            input =  GetParameter(0).Evaluate();
        }
        return Mathf.Round(input);
    }
}
