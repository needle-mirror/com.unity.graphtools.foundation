using System;
using System.Collections.Generic;

namespace UnityEditor.VisualScripting.Model
{
    public interface IHasVariableDeclaration
    {
        IList<VariableDeclarationModel> VariableDeclarations { get; }
    }
}
