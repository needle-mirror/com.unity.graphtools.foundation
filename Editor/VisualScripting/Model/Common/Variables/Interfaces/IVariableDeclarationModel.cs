using System;
using System.Linq;
using JetBrains.Annotations;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model.Stencils;
using UnityEditor.VisualScripting.Model.Translators;

namespace UnityEditor.VisualScripting.Model
{
    public interface IVariableDeclarationModel : IGraphElementModelWithGuid
    {
        string Title { get; }
        string Name { get; }
        VariableType VariableType { get; }
        string VariableName { get; }
        TypeHandle DataType { get; }
        bool IsExposed { get; }
        IConstantNodeModel InitializationModel { get; }
        IHasVariableDeclaration Owner { get; }
        ModifierFlags Modifiers { get; }
        string Tooltip { get; }
    }
}
