using System;
using UnityEditor.EditorCommon.Redux;

namespace UnityEditor.CodeViewer
{
    class ChangeDocumentAction : object, IAction
    {
        public Document Document { get; }

        public ChangeDocumentAction(Document document)
        {
            Document = document;
        }
    }

    class ActivateLineAction : object, IAction
    {
        public Line Line { get; }

        public ActivateLineAction(Line line)
        {
            Line = line;
        }
    }

    class ToggleShowLineNumberAction : object, IAction
    {
    }

    class ToggleShowLineIconsAction : object, IAction
    {
    }

    class ChangeLockStateAction : object, IAction
    {
        public bool Locked { get; }

        public ChangeLockStateAction(bool locked)
        {
            Locked = locked;
        }
    }
}
