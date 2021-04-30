namespace UnityEngine.GraphToolsFoundation.Overdrive
{
    public interface ITypeMetadataResolver
    {
        ITypeMetadata Resolve(TypeHandle th);
    }
}
