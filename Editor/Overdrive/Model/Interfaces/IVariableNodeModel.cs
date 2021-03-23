namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public interface IVariableNodeModel : ISingleInputPortNodeModel, ISingleOutputPortNodeModel, IHasDeclarationModel, IHasTitle
    {
        IVariableDeclarationModel VariableDeclarationModel { get; }
        void UpdateTypeFromDeclaration();
    }

    public static class IVariableNodeModelExtensions
    {
        public static TypeHandle GetDataType(this IVariableNodeModel self) =>
            self.VariableDeclarationModel?.DataType ?? TypeHandle.Unknown;
    }
}
