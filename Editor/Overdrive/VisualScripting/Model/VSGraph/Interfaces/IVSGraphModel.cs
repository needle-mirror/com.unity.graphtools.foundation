using System;
using System.Collections.Generic;
using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.GraphViewModel;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting
{
    public interface IVSGraphModel : IGraphModel, IHasVariableDeclaration
    {
        IEnumerable<IVariableDeclarationModel> GraphVariableModels { get; }

        IEnumerable<IVariableDeclarationModel> GraphPortalModels { get; }
    }
}
