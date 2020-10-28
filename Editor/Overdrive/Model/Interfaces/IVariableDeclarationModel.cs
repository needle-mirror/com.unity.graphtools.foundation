using System;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public enum VariableType
    {
        GraphVariable,
        EdgePortal
    }

    [Flags]
    public enum ModifierFlags
    {
        None      = 0,
        ReadOnly  = 1 << 0,
        WriteOnly = 1 << 1,
        ReadWrite = 1 << 2,
    }

    public interface IVariableDeclarationMetadataModel{}

    public interface IVariableDeclarationModel : IDeclarationModel
    {
        VariableType VariableType { get; }
        TypeHandle DataType { get; set; }
        ModifierFlags Modifiers { get; }
        string VariableName { get; }
        string Tooltip { get; set; }
        IConstant InitializationModel { get; }
        // Is the variable available in the game object inspector?
        bool IsExposed { get; set; }
        void SetGraphModel(IGraphModel model);
        void CreateInitializationValue();
        T GetMetadataModel<T>() where T : IVariableDeclarationMetadataModel;
        void SetMetadataModel<T>(T value) where T : IVariableDeclarationMetadataModel;
    }
}
