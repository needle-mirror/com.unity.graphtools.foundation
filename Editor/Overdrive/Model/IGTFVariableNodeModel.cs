using System;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Model
{
    public interface IGTFVariableNodeModel : IGTFNodeModel, IHasSingleInputPort, IHasSingleOutputPort
    {
        IGTFVariableDeclarationModel VariableDeclarationModel { get; }
    }
}
