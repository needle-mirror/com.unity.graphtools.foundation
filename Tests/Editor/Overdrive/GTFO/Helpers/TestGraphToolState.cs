using System;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GTFO
{
    public class TestGraphToolState : GraphToolState
    {
        static Preferences CreatePreferences()
        {
            var prefs = Preferences.CreatePreferences("GraphToolsFoundationTests.");
            prefs.SetBoolNoEditorUpdate(BoolPref.ErrorOnRecursiveDispatch, false);
            prefs.SetBoolNoEditorUpdate(BoolPref.ErrorOnMultipleDispatchesPerFrame, false);
            return prefs;
        }

        IGraphModel m_GraphModel;
        public override IGraphModel GraphModel => m_GraphModel;

        public TestGraphToolState(GUID graphViewEditorWindowGUID, IGraphModel graphModel)
            : base(graphViewEditorWindowGUID, CreatePreferences())
        {
            m_GraphModel = graphModel;
        }

        ~TestGraphToolState() => Dispose(false);
    }
}
