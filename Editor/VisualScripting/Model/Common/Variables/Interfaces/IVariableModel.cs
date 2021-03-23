using System;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEngine;

namespace UnityEditor.VisualScripting.Model
{
    public interface IVariableModel : IHasMainOutputPort
    {
        IVariableDeclarationModel DeclarationModel { get; }
    }
}
