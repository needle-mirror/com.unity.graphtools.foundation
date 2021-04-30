using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public class GraphProcessingResult
    {
        readonly List<GraphProcessingError> m_Errors = new List<GraphProcessingError>();

        public IReadOnlyList<GraphProcessingError> Errors => m_Errors;
        public GraphProcessingStatus Status => Errors.Any(e => e.IsWarning == false) ?
        GraphProcessingStatus.Failed : GraphProcessingStatus.Succeeded;

        public void AddError(string description, INodeModel node = null, QuickFix quickFix = null)
        {
            AddError(description, node, false, quickFix);
        }

        public void AddWarning(string description, INodeModel node = null, QuickFix quickFix = null)
        {
            AddError(description, node, true, quickFix);
        }

        void AddError(string desc, INodeModel node, bool isWarning, QuickFix quickFix)
        {
            m_Errors.Add(new GraphProcessingError { Description = desc, SourceNode = node, SourceNodeGuid = node == null ? default : node.Guid, IsWarning = isWarning, Fix = quickFix });
        }
    }
}
