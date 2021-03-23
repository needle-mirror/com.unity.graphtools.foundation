using System;
using UnityEditor.EditorCommon.Redux;

namespace UnityEditor.CodeViewer
{
    class Store : Store<CodeViewerState>
    {
        public Store(CodeViewerState state) : base(state)
        {
        }
    }
}
