using System;

namespace UnityEditor.CodeViewer
{
    class CodeViewerState : IDisposable
    {
        public IDocument Document { get; set; }
        public IEditorData ViewerSettings { get; private set; }

        public CodeViewerState()
        {
            ViewerSettings = new ViewerSettings();
        }

        public void Dispose()
        {
            Document = null;
            ViewerSettings = null;
        }
    }
}
