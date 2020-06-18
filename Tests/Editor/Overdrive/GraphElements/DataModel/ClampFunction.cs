using UnityEngine;

public class ClampFunction : MathFunction
{
    void OnEnable()
    {
        name = "Clamp";
        if (m_ParameterIDs.Length == 0)
        {
            m_ParameterIDs = new MathNodeID[3];
        }

        if (m_ParameterNames.Length == 0)
        {
            m_ParameterNames = new string[] { "val", "min", "max" };
        }
    }

    public override float Evaluate()
    {
        float val = 0.0f;
        float min = 0.0f;
        float max = 0.0f;
        if (GetParameter(0) != null)
        {
            val =  GetParameter(0).Evaluate();
        }
        if (GetParameter(1) != null)
        {
            min =  GetParameter(1).Evaluate();
        }
        if (GetParameter(2) != null)
        {
            max =  GetParameter(2).Evaluate();
        }
        return Mathf.Clamp(val, min, max);
    }
}
