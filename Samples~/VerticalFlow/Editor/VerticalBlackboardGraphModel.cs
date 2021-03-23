using System.Collections.Generic;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.Vertical
{
    class VerticalBlackboardGraphModel : BlackboardGraphModel
    {
        public override string GetBlackboardTitle()
        {
            return "Vertical Flow";
        }

        public override IEnumerable<IVariableDeclarationModel> GetSectionRows(string sectionName)
        {
            return Enumerable.Empty<IVariableDeclarationModel>();
        }

        public override void PopulateCreateMenu(string sectionName, GenericMenu menu, CommandDispatcher commandDispatcher)
        {
        }
    }
}
