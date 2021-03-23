using System;

namespace UnityEditor.CodeViewer
{
    interface IEditorData
    {
        bool ShowLineNumber { get; }
        bool ShowLineIcons { get; }
        bool SelectionLocked { get; }
    }

    class ViewerSettings : IEditorData
    {
        public bool ShowLineNumber { get; set; }
        public bool ShowLineIcons { get; set; }
        public bool SelectionLocked { get; set; }

        public ViewerSettings()
        {
            ShowLineNumber = true;
            ShowLineIcons = true;
            SelectionLocked = false;
        }
    }
}
