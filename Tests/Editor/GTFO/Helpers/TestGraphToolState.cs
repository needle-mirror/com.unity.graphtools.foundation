using System;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GTFO
{
    class WindowStateComponent : Overdrive.WindowStateComponent
    {
        internal IGraphModel m_GraphModel;

        public override IGraphModel GraphModel => m_GraphModel;
    }

    class GraphViewStateComponent : Overdrive.GraphViewStateComponent
    {
        internal IGraphModel m_GraphModel;

        public override IGraphModel GraphModel => m_GraphModel;
    }

    class TestGraphToolState : GraphToolState
    {
        IGraphModel m_GraphModel;

        static Preferences CreatePreferences()
        {
            var prefs = Preferences.CreatePreferences("GraphToolsFoundationTests.");
            prefs.SetBoolNoEditorUpdate(BoolPref.ErrorOnRecursiveDispatch, false);
            return prefs;
        }

        private protected override Overdrive.WindowStateComponent CreateWindowStateComponent(Hash128 guid)
        {
            var state = PersistedState.GetOrCreateViewStateComponent<WindowStateComponent>(guid, nameof(WindowState));
            state.m_GraphModel = m_GraphModel;
            return state;
        }

        private protected override Overdrive.GraphViewStateComponent CreateGraphViewStateComponent()
        {
            var state = PersistedState.GetOrCreateAssetStateComponent<GraphViewStateComponent>(nameof(GraphViewState));
            state.m_GraphModel = m_GraphModel;
            return state;
        }

        public TestGraphToolState(SerializableGUID graphViewEditorWindowGUID, IGraphModel graphModel)
            : base(graphViewEditorWindowGUID, CreatePreferences())
        {
            m_GraphModel = graphModel;
        }

        ~TestGraphToolState() => Dispose(false);
    }
}
