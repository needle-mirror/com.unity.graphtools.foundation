using System;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.BasicModel
{
    public class GraphProcessingErrorBadgeModel : ErrorBadgeModel
    {
        public QuickFix Fix { get; }

        public GraphProcessingErrorBadgeModel(GraphProcessingError error)
            : base(error.SourceNode)
        {
            m_ErrorMessage = error.Description;
            Fix = error.Fix;
        }
    }
}
