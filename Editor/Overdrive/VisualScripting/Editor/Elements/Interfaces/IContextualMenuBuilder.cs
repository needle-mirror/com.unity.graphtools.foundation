using System;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting
{
    public interface IContextualMenuBuilder
    {
        void BuildContextualMenu(ContextualMenuPopulateEvent evt);
    }
}
