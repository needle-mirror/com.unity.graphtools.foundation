using System;
using System.Collections.Generic;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model.Stencils;

namespace UnityEditor.VisualScripting.Model
{
    public interface IFunctionModel : IStackModel, IHasVariableDeclaration
    {
        IEnumerable<IVariableDeclarationModel> FunctionVariableModels { get; }
        IEnumerable<IVariableDeclarationModel> FunctionParameterModels { get; }
        TypeHandle ReturnType { get; }
        bool IsEntryPoint { get; }
        string CodeTitle { get; }
        bool AllowChangesToModel { get; }
        bool AllowMultipleInstances { get; }
        bool EnableProfiling { get; }
        bool HasReturnType { get; }
    }
}
