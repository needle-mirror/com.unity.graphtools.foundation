using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor.VisualScripting.GraphViewModel;

namespace UnityEditor.VisualScripting.Model
{
    public interface IFunctionCallModel : IHasMainOutputPort
    {
        IEnumerable<string> ParametersNames { get; }
        IPortModel GetPortForParameter(string parameterName);
    }

    public static class IFunctionCallHelper
    {
        public static IEnumerable<IPortModel> GetParameterPorts(this IFunctionCallModel functionCallModel)
        {
            return functionCallModel.ParametersNames.Select(functionCallModel.GetPortForParameter);
        }
    }
}
