using System;
using UnityEngine;

namespace UnityEditor.VisualScripting.Editor
{
    public interface ICustomColor
    {
        void ResetColor();

        void SetColor(Color c);
    }
}
