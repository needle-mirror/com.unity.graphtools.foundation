using System;
using System.Collections.Concurrent;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public class TypeMetadataResolver : ITypeMetadataResolver
    {
        readonly ConcurrentDictionary<TypeHandle, ITypeMetadata> m_MetadataCache
            = new ConcurrentDictionary<TypeHandle, ITypeMetadata>();

        public ITypeMetadata Resolve(TypeHandle th)
        {
            if (!m_MetadataCache.TryGetValue(th, out ITypeMetadata metadata))
            {
                metadata = m_MetadataCache.GetOrAdd(th, t => new TypeMetadata(t, th.Resolve()));
            }
            return metadata;
        }
    }
}
