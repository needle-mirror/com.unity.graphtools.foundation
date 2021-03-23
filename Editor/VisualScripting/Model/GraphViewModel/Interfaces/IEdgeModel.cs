using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace UnityEditor.VisualScripting.GraphViewModel
{
    public interface IEdgeModel : IGraphElementModel
    {
        string OutputId { get; }
        string InputId { get; }
        GUID InputNodeGuid { get; }
        GUID OutputNodeGuid { get; }
        IPortModel InputPortModel { get; }
        IPortModel OutputPortModel { get; }
        string EdgeLabel { get; }
    }

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public static class IEdgeModelExtensions
    {
        public static IEnumerable<IPortModel> GetPortModels(this IEdgeModel edge)
        {
            yield return edge.InputPortModel;
            yield return edge.OutputPortModel;
        }

        public static bool IsValid(this IEdgeModel edge)
        {
            return edge.InputPortModel != null && edge.OutputPortModel != null;
        }
    }
}
