using System;
using UnityEditor.GraphToolsFoundation.Overdrive.GraphElements;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting
{
    public static class RenamableVariableHelper
    {
        internal static void EnableRename(this IRenamable renamable)
        {
            var clickable = new SimpleClickable(renamable.Rename);
            clickable.activators.Clear();
            clickable.activators.Add(
                new ManipulatorActivationFilter {button = MouseButton.LeftMouse, clickCount = 1});
            renamable.TitleElement.AddManipulator(clickable);
        }

        static void Rename(this IRenamable renamable, MouseDownEvent mouseDownEvent = null)
        {
            Rename(renamable, false, mouseDownEvent);
        }

        internal static void Rename(this IRenamable renamable, bool forceRename, MouseDownEvent mouseDownEvent = null)
        {
            if (renamable.RenameDelegate != null)
            {
                renamable.RenameDelegate();
                return;
            }

            var graphElement = (GraphElement)renamable;
            if (!graphElement.IsRenamable())
                return;

            var timeDelta = EditorApplication.timeSinceStartup - VseGraphView.clickTimeSinceStartupSecs;
            if (!forceRename && (graphElement != VseGraphView.clickTarget ||
                                 !(graphElement is IDoubleClickable) && mouseDownEvent != null && mouseDownEvent.clickCount != 2 ||
                                 graphElement is IDoubleClickable && (timeDelta < VseGraphView.SlowDoubleClickSecs || timeDelta > VseGraphView.SlowDoubleClickMaxTimeElapsed)))
            {
                if (graphElement is IDoubleClickable && timeDelta > VseGraphView.SlowDoubleClickMaxTimeElapsed)
                    VseGraphView.clickTimeSinceStartupSecs = EditorApplication.timeSinceStartup;
                return;
            }

            if (!(renamable.TitleEditor is TextField textField))
                return;

            textField.value = renamable.TitleValue;
            renamable.EditTitleCancelled = false;
            renamable.TitleEditor.RegisterCallback<FocusEvent>(renamable.OnFocus);
            renamable.TitleEditor.RegisterCallback<FocusOutEvent>(renamable.OnFocusOut);
            renamable.TitleEditor.Q(TextInputBaseField<string>.textInputUssName).RegisterCallback<KeyDownEvent>(OnKeyPressed);
            renamable.TitleEditor.StretchToParentSize();
            renamable.TitleElement.Add(renamable.TitleEditor);
            var textInput = renamable.TitleEditor.Q(TextInputBaseField<string>.textInputUssName);
            textInput.Focus();
            mouseDownEvent?.StopImmediatePropagation();
        }

        static void OnKeyPressed(KeyDownEvent evt)
        {
            var renamable = ((VisualElement)evt.target).GetFirstAncestorOfType<IRenamable>();
            Assert.IsNotNull(renamable);

            switch (evt.keyCode)
            {
                case KeyCode.Escape:
                    renamable.EditTitleCancelled = true;
                    renamable.TitleEditor.Blur();
                    break;
                case KeyCode.Return:
                    renamable.TitleEditor.Blur();
                    break;
            }
        }

        static void OnFocusOut(this IRenamable renamable, FocusOutEvent evt)
        {
            VseWindow window = renamable.TitleElement.GetFirstAncestorOfType<VseGraphView>()?.window;
            if (window != null)
                window.RefreshUIDisabled = false;

            renamable.TitleEditor.UnregisterCallback<FocusEvent>(renamable.OnFocus);
            renamable.TitleEditor.UnregisterCallback<FocusOutEvent>(renamable.OnFocusOut);
            renamable.TitleEditor.Q(TextInputBaseField<string>.textInputUssName).UnregisterCallback<KeyDownEvent>(OnKeyPressed);

            // ReSharper disable once DelegateSubtraction
            Undo.undoRedoPerformed -= renamable.UndoRedoPerformed;

            renamable.TitleEditor.RemoveFromHierarchy();

            if (!renamable.EditTitleCancelled)
            {
                if (renamable.TitleEditor is TextField textField && renamable.TitleValue != textField.text)
                    renamable.Store.Dispatch(new RenameElementAction((UnityEditor.GraphToolsFoundation.Overdrive.Model.IRenamable)renamable.GraphElementModel, textField.text));
            }
            else
            {
                renamable.EditTitleCancelled = false;
            }
        }

        static void OnFocus(this IRenamable renamable, FocusEvent evt)
        {
            var textField = renamable.TitleEditor as TextField;
            textField?.SelectAll();

            VseWindow window = renamable.TitleElement.GetFirstAncestorOfType<VseGraphView>()?.window;
            // OnBlur is not called after a function is created in a new window and the window is closed, e.g. in tests
            ((VisualElement)renamable).RegisterCallback<DetachFromPanelEvent>(Callback);
            if (window != null)
                window.RefreshUIDisabled = true;

            renamable.TitleEditor.UnregisterCallback<FocusEvent>(renamable.OnFocus);

            Undo.undoRedoPerformed += renamable.UndoRedoPerformed;
        }

        static void Callback<T>(EventBase<T> evt) where T : EventBase<T>, new()
        {
            var renamable = ((IRenamable)evt.target);
            // ReSharper disable once DelegateSubtraction
            Undo.undoRedoPerformed -= renamable.UndoRedoPerformed;

            renamable.TitleEditor.UnregisterCallback<FocusEvent>(renamable.OnFocus);
            renamable.TitleEditor.UnregisterCallback<FocusOutEvent>(renamable.OnFocusOut);
            renamable.TitleEditor.Q(TextInputBaseField<string>.textInputUssName).UnregisterCallback<KeyDownEvent>(OnKeyPressed);
        }

        static void UndoRedoPerformed(this IRenamable renamable)
        {
            var textField = renamable.TitleEditor as TextField;
            if (textField == null)
                return;

            textField.value = renamable.TitleValue;
            renamable.TitleEditor.Blur();
        }
    }
}
