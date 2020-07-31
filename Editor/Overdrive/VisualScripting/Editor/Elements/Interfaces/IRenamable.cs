using System;
using UnityEditor.GraphToolsFoundation.Overdrive.GraphElements;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting
{
    public delegate void RenameDelegate();

    public interface IRenamable : IGraphElement
    {
        string TitleValue { get; }
        VisualElement TitleEditor { get; }
        VisualElement TitleElement { get; }

        bool IsFramable();

        bool EditTitleCancelled { get; set; }

        RenameDelegate RenameDelegate { get; }
    }
}
