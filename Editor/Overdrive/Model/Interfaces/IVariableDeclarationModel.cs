using System;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    [Flags]
    public enum ModifierFlags
    {
        None = 0,
        ReadOnly = 1 << 0,
        WriteOnly = 1 << 1,
        ReadWrite = 1 << 2,
    }

    public interface IVariableDeclarationModel : IDeclarationModel
    {
        TypeHandle DataType { get; set; }
        /// <summary>
        /// The read/write modifiers.
        /// </summary>
        ModifierFlags Modifiers { get; set; }
        string Tooltip { get; set; }
        IConstant InitializationModel { get; }
        // Is the variable available in the game object inspector?
        bool IsExposed { get; set; }
        /// <summary>
        /// Get the name of the variable with non-alphanumeric characters replaced by an underscore.
        /// </summary>
        /// <returns>The name of the variable with non-alphanumeric characters replaced by an underscore.</returns>
        string GetVariableName();
        void CreateInitializationValue();
    }
}
