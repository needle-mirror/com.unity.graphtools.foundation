using System;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Model
{
    public interface IGTFVariableNodeModel : ISingleInputPortNode, ISingleOutputPortNode
    {
        IGTFVariableDeclarationModel VariableDeclarationModel { get; }
    }
}
