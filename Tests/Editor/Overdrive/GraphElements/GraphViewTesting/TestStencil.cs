using System;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GraphElements
{
    public class TestStencil : Stencil
    {
        public override Type GetConstantNodeValueType(TypeHandle typeHandle)
        {
            return TypeToConstantMapper.GetConstantNodeType(typeHandle);
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
