using System;
using System.Collections.Generic;
using UnityEditor.VisualScripting.GraphViewModel;

namespace UnityEditor.VisualScripting.Model
{
    public interface IVSGraphModel : IGraphModel, IHasVariableDeclaration
    {
        IEnumerable<IStackModel> StackModels { get; }
        IEnumerable<IVariableDeclarationModel> GraphVariableModels { get; }
    }
}
