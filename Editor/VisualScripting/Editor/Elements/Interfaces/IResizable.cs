#if !UNITY_2020_1_OR_NEWER
using System;

namespace UnityEditor.VisualScripting.Editor
{
    public interface IResizable
    {
        void OnStartResize();
        void OnResized();
    }
}
#endif
