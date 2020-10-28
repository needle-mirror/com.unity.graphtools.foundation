using System;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public interface IDestroyable
    {
        bool Destroyed { get; }
        void Destroy();
    }
}
