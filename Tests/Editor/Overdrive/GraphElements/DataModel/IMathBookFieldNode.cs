using System;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GraphElements
{
    public interface IMathBookFieldNode
    {
        event Action<IMathBookFieldNode> changed;

        string fieldName { get; set; }
        MathBookField.Direction direction { get; }
        MathBookField field { get; }

        void NotifyChange();
    }
}
