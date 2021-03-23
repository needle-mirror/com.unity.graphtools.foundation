using System;
using UnityEngine.UIElements;

namespace UnityEditor.VisualScripting.Editor
{
    public interface IContextualMenuBuilder
    {
        void BuildContextualMenu(ContextualMenuPopulateEvent evt);
    }
}
