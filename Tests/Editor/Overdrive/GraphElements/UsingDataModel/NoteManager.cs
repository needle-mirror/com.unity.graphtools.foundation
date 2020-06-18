#if DISABLE_SIMPLE_MATH_TESTS
using Unity.GraphElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Editor.UsingDataModel.NoPresenters
{
    static class NoteManager
    {
        static string GetMessage(string messageType, ref int messageCount)
        {
            switch (messageCount++ % 3)
            {
                case 0:
                    return string.Format("This is {0} message.", messageType);
                case 1:
                    return string.Format("This is {0} long message with hard-coded end-of-line:\r\nfirst line\r\nsecond line\r\nyou get the idea.", messageType);
                default:
                    return string.Format("This is {0} message that should wrap on multiple lines. Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.", messageType);
            }
        }

        static int errorMessageCount = 0;

        static public void CreateErrorNote(VisualElement target, SpriteAlignment align)
        {
            AttachNote(target, IconBadge.CreateError(GetMessage("an error", ref errorMessageCount)), align);
        }

        static int commentMessageCount = 0;
        static public void CreateCommentNote(VisualElement target, SpriteAlignment align)
        {
            AttachNote(target, IconBadge.CreateComment(GetMessage("a comment", ref commentMessageCount)), align);
        }

        static void AttachNote(VisualElement target, IconBadge note, SpriteAlignment align)
        {
            VisualElement noteParent = target.GetFirstOfType<Node>();

            if (noteParent == null)
            {
                noteParent = target.parent;
            }

            if (target is Node)
            {
                noteParent = target;
            }

            noteParent.Add(note);
            note.AttachTo(target, align);
            note.style.position = Position.Absolute;

            note.AddManipulator(new Clickable(() =>
            {
                note.Detach();
                note.RemoveFromHierarchy();
            }));
        }
    }
}
#endif
