using System;

namespace UnityEditor.VisualScripting.Editor
{
    public class MissingUIElementException : Exception
    {
        public MissingUIElementException(string message)
            : base(message)
        {
        }
    }
}
