using UnityEngine;

public class PIConstant : MathConstant
{
    public new void OnEnable()
    {
        name = "PI";
        m_Value = Mathf.PI;
    }
}
