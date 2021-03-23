using System;

namespace UnityEditor.VisualScripting.Model.Translators
{
    public class LoopDetectedException : Exception
    {
        public LoopDetectedException(string message)
            : base(message) { }
    }
}
