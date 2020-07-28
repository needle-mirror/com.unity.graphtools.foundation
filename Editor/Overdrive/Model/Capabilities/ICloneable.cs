using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Model
{
    public interface ICloneable : IGTFGraphElementModel
    {
        IGTFGraphElementModel Clone();
    }
}
