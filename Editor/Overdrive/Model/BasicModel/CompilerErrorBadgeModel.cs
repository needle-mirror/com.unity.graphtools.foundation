using System;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.BasicModel
{
    public class CompilerErrorBadgeModel : ErrorBadgeModel
    {
        public CompilerQuickFix QuickFix;

        public CompilerErrorBadgeModel(CompilerError error)
            : base(error.sourceNode)
        {
            m_ErrorMessage = error.description;
            QuickFix = error.quickFix;
        }
    }
}
