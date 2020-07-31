using System;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Model
{
    public interface IDestroyable
    {
        bool Destroyed { get; }
        void Destroy();
    }
}
