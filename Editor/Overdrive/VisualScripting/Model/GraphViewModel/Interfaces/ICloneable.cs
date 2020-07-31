using System;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.GraphViewModel
{
    public interface ICloneable : IGTFGraphElementModel
    {
        IGTFGraphElementModel Clone();
    }
}
