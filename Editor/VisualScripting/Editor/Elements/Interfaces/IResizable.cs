using System;

namespace UnityEditor.VisualScripting.Editor
{
    public interface IResizable
    {
        void OnStartResize();
        void OnResized();
    }
}
