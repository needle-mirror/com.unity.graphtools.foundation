using System;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public interface ITypeMetadataResolver
    {
        ITypeMetadata Resolve(TypeHandle th);
    }
}
