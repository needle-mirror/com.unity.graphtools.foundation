using System;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model.Stencils;

namespace UnityEditor.VisualScripting.Model
{
    public interface IOperationValidator
    {
        bool HasValidOperationForInput(IPortModel inputPort, TypeHandle typeHandle);
    }
}
