using System;
using System.Collections.Concurrent;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting
{
    public class TypeMetadataResolver : ITypeMetadataResolver
    {
        readonly ConcurrentDictionary<TypeHandle, ITypeMetadata> m_MetadataCache
            = new ConcurrentDictionary<TypeHandle, ITypeMetadata>();
        GraphContext m_GraphContext;
        public TypeMetadataResolver(GraphContext graphContext)
        {
            m_GraphContext = graphContext;
        }

        public ITypeMetadata Resolve(TypeHandle th)
        {
            if (!m_MetadataCache.TryGetValue(th, out ITypeMetadata metadata))
            {
                metadata = m_MetadataCache.GetOrAdd(th, t => new CSharpTypeBasedMetadata(m_GraphContext, t, th.Resolve(m_GraphContext.CSharpTypeSerializer)));
            }
            return metadata;
        }
    }

    public interface ITypeMetadataResolver
    {
        ITypeMetadata Resolve(TypeHandle th);
    }
}
