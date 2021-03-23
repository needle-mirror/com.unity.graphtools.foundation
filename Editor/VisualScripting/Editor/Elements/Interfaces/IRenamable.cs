using System;
using UnityEngine.UIElements;

namespace UnityEditor.VisualScripting.Editor
{
    public delegate void RenameDelegate();

    public interface IRenamable : IHasGraphElementModel
    {
        Store Store { get; }
        string TitleValue { get; }
        VisualElement TitleEditor { get; }
        VisualElement TitleElement { get; }

        bool IsFramable();

        bool EditTitleCancelled { get; set; }

        RenameDelegate RenameDelegate { get; }
    }
}
