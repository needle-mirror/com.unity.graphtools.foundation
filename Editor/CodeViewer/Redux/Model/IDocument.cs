using System;
using System.Collections.Generic;

namespace UnityEditor.CodeViewer
{
    interface IDocument
    {
        IReadOnlyList<ILine> Lines { get; }
        Action<object> Callback { get; }
        string FullText { get; }
    }
}
