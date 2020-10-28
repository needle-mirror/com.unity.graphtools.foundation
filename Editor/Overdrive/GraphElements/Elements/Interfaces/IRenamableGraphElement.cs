using System;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public delegate void RenameDelegate();

    public interface IRenamableGraphElement : IGraphElement
    {
        string TitleValue { get; }
        VisualElement TitleEditor { get; }
        VisualElement TitleElement { get; }

        bool IsFramable();

        bool EditTitleCancelled { get; set; }

        RenameDelegate RenameDelegate { get; }
    }
}
