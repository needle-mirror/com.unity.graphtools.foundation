using System;
using System.Collections.Generic;

namespace UnityEditor.CodeViewer
{
    public interface ILine
    {
        int LineNumber { get; }
        string Text { get; }
        IReadOnlyList<ILineDecorator> Decorators { get; }
    }
}
