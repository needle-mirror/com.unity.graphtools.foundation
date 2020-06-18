using System;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting
{
    public class MissingUIElementException : Exception
    {
        public MissingUIElementException(string message)
            : base(message)
        {
        }
    }
}
