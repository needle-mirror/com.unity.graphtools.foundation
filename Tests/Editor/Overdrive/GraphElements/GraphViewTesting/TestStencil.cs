using System;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GraphElements
{
    public class TestStencil : Stencil
    {
        public override Blackboard CreateBlackboard(Store store, GraphView graphView)
        {
            return null;
        }

        Action m_OnGetSearcherDatabaseProviderCallback;
        public override ISearcherDatabaseProvider GetSearcherDatabaseProvider()
        {
            if (m_OnGetSearcherDatabaseProviderCallback != null)
                m_OnGetSearcherDatabaseProviderCallback.Invoke();

            return null;
        }

        public void SetOnGetSearcherDatabaseProviderCallback(Action callback)
        {
            m_OnGetSearcherDatabaseProviderCallback = callback;
        }
    }
}
