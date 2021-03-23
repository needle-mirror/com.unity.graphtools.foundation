using System;

namespace UnityEditor.VisualScripting.Editor
{
    public interface IMovable
    {
        void UpdatePinning();
        bool NeedStoreDispatch { get; }
    }
}
