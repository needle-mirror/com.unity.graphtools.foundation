using System;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.GraphViewModel;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting
{
    public interface IVariableModel : IHasMainOutputPort, IHasSingleInputPort, IHasSingleOutputPort
    {
        IVariableDeclarationModel DeclarationModel { get; }
    }
}
