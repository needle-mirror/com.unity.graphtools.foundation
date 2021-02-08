using System;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public class QuickFix
    {
        public string Description { get; }
        public Action<CommandDispatcher> QuickFixAction { get; }

        public QuickFix(string description, Action<CommandDispatcher> quickFixAction)
        {
            Description = description;
            QuickFixAction = quickFixAction;
        }
    }
}
