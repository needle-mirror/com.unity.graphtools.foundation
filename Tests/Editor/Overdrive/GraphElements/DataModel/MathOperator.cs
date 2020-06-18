using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class MathOperator : MathNode
{
    // Inputs
    [SerializeField]
    private MathNodeID m_LeftID = MathNodeID.empty;  // Anchor single input

    [SerializeField]
    private MathNodeID m_RightID = MathNodeID.empty; // Anchor single input

    public MathNode left
    {
        get
        {
            return m_LeftID.Get(mathBook);
        }
        set
        {
            m_LeftID.Set(value);
        }
    }

    public MathNode right
    {
        get
        {
            return m_RightID.Get(mathBook);
        }
        set
        {
            m_RightID.Set(value);
        }
    }

    public override void ResetConnections()
    {
        left = right = null;
    }

    public override void RemapReferences(Dictionary<string, string> oldIDNewIDMap)
    {
        base.RemapReferences(oldIDNewIDMap);

        RemapID(oldIDNewIDMap, ref m_LeftID);
        RemapID(oldIDNewIDMap, ref m_RightID);
    }
}
