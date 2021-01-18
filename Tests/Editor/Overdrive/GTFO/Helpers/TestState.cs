using System;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GTFO
{
    public class TestState : State
    {
        static Preferences CreatePreferences()
        {
            var prefs = TestPreferences.CreatePreferences();
            prefs.SetBoolNoEditorUpdate(BoolPref.ErrorOnRecursiveDispatch, false);
            prefs.SetBoolNoEditorUpdate(BoolPref.ErrorOnMultipleDispatchesPerFrame, false);
            return prefs;
        }

        IGraphModel m_GraphModel;
        public override IGraphModel GraphModel => m_GraphModel;

        public TestState(GUID graphViewEditorWindowGUID, IGraphModel graphModel)
            : base(graphViewEditorWindowGUID, CreatePreferences())
        {
            m_GraphModel = graphModel;
        }

        ~TestState() => Dispose(false);
    }
}
