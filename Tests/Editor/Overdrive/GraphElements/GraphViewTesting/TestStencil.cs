using System;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GraphElements
{
    public class TestStencil : Stencil
    {
        public static string toolName = "GTF GraphElements Tests";

        public override string ToolName => toolName;

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

        /// <inheritdoc />
        public override IBlackboardGraphModel CreateBlackboardGraphModel(IGraphAssetModel graphAssetModel)
        {
            return new BlackboardGraphModel(graphAssetModel);
        }
    }
}
