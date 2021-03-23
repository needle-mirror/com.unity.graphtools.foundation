using System;

namespace UnityEditor.CodeViewer
{
    static class Reducers
    {
        public static void Register(Store store)
        {
            store.Register<ChangeDocumentAction>(ChangeDocument);
            store.Register<ActivateLineAction>(ActivateLine);
            store.Register<ToggleShowLineNumberAction>(ToggleShowLineNumber);
            store.Register<ToggleShowLineIconsAction>(ToggleShowLineIcons);
            store.Register<ChangeLockStateAction>(ChangeLockState);
        }

        static CodeViewerState ChangeDocument(CodeViewerState previousState, ChangeDocumentAction action)
        {
            if (!previousState.ViewerSettings.SelectionLocked)
            {
                previousState.Document = action.Document;
            }
            return previousState;
        }

        static CodeViewerState ActivateLine(CodeViewerState previousState, ActivateLineAction action)
        {
            previousState.Document.Callback?.Invoke(action.Line.Metadata);
            return previousState;
        }

        static CodeViewerState ToggleShowLineNumber(CodeViewerState previousState, ToggleShowLineNumberAction action)
        {
            ((ViewerSettings)previousState.ViewerSettings).ShowLineNumber = !previousState.ViewerSettings.ShowLineNumber;
            return previousState;
        }

        static CodeViewerState ToggleShowLineIcons(CodeViewerState previousState, ToggleShowLineIconsAction action)
        {
            ((ViewerSettings)previousState.ViewerSettings).ShowLineIcons = !previousState.ViewerSettings.ShowLineIcons;
            return previousState;
        }

        static CodeViewerState ChangeLockState(CodeViewerState previousState, ChangeLockStateAction action)
        {
            ((ViewerSettings)previousState.ViewerSettings).SelectionLocked = action.Locked;
            return previousState;
        }
    }
}
