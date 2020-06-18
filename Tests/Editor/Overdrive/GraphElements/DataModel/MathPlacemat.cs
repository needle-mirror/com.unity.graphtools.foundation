using System;
using System.Collections.Generic;
using UnityEngine;

public class MathPlacemat : ScriptableObject
{
    public MathBook mathBook { get; set; }


    [SerializeField]
    Rect m_Position;

    public Rect position
    {
        get => m_Position;
        set => m_Position = value;
    }

    [SerializeField]
    Vector2 m_UncollapsedSize;

    public Vector2 uncollapsedSize
    {
        get => m_UncollapsedSize;
        set => m_UncollapsedSize = value;
    }

    [SerializeField]
    string m_Title;

    public string title
    {
        get => m_Title;
        set => m_Title = value;
    }

    [SerializeField]
    string m_Identification;

    public string identification
    {
        get => m_Identification;
        set => m_Identification = value;
    }

    public string RewriteID()
    {
        m_Identification = Guid.NewGuid().ToString();
        return m_Identification;
    }

    public void RemapReferences(Dictionary<string, string> idMap)
    {
        if (Collapsed)
        {
            var collapsedIds = new List<string>();
            foreach (var hiddenElement in HiddenElementsId)
            {
                string newId;
                newId = idMap.TryGetValue(hiddenElement, out newId) ? newId : hiddenElement;
                collapsedIds.Add(newId);
            }

            HiddenElementsId = collapsedIds;
        }
    }

    [SerializeField]
    int m_ZOrder;

    public int zOrder
    {
        get => m_ZOrder;
        set => m_ZOrder = value;
    }

    [SerializeField]
    Color m_Color;

    public Color Color
    {
        get => m_Color;
        set => m_Color = value;
    }

    [SerializeField]
    bool m_Collapsed;

    public bool Collapsed
    {
        get => m_Collapsed;
        set => m_Collapsed = value;
    }

    [SerializeField]
    List<string> m_HiddenElementIds;

    public List<string> HiddenElementsId
    {
        get => m_HiddenElementIds;
        set => m_HiddenElementIds = value;
    }

    public static MathPlacemat CreateInstance(Rect position, int zOrder)
    {
        MathPlacemat inst = CreateInstance<MathPlacemat>();
        inst.m_Title = "New Placemat";
        inst.m_Position = position;
        inst.m_Collapsed = false;
        inst.m_Color = new Color(0.15f, 0.19f, 0.19f);
        inst.m_Identification = Guid.NewGuid().ToString();
        inst.m_ZOrder = zOrder;
        return inst;
    }
}
