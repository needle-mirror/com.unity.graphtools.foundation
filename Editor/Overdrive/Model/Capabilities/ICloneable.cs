using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public interface ICloneable : IGraphElementModel
    {
        IGraphElementModel Clone();
    }
}
