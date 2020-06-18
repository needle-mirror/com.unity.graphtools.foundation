using System;
using System.Collections.Generic;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting
{
    public interface IHasVariableDeclaration
    {
        IList<VariableDeclarationModel> VariableDeclarations { get; }
    }
}
