using System;
using UnityEditor.VisualScripting.GraphViewModel;

namespace UnityEditor.VisualScripting.Model.Stencils
{
    public interface IOrderedStack : IStackModel
    {
        int Order { get; set; }
    }
}
