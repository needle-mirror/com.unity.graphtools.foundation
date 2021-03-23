using System;
using UnityEngine;

namespace UnityEditor.CodeViewer
{
    public interface ILineDecorator
    {
        Texture2D Icon { get; }
        string Tooltip { get; }
    }
}
