using System;
using UnityEngine;

public class MathStickyNote : ScriptableObject
{
    public MathBook mathBook { get; set; }

    [SerializeField]
    string m_ID;
    public string id { get => m_ID; }

    [SerializeField]
    private Rect m_Position;
    public Rect position { get => m_Position; set => m_Position = value; }

    [SerializeField]
    private string m_Theme;
    public string theme { get => m_Theme; set => m_Theme = value; }

    [SerializeField]
    private string m_Title;
    public string title { get => m_Title; set => m_Title = value; }

    [SerializeField]
    private string m_Contents;
    public string contents { get => m_Contents; set => m_Contents = value; }

    [SerializeField]
    private string m_TextSize;
    public string textSize { get => m_TextSize; set => m_TextSize = value; }

    protected MathStickyNote()
    {
        m_ID = Guid.NewGuid().ToString();
    }

    public void RewriteID()
    {
        m_ID = Guid.NewGuid().ToString();
    }
}
