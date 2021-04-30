using System;
using System.Collections.Generic;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public interface IBlackboardGraphModel : IGraphElementModel
    {
        bool Valid { get; }
        string GetBlackboardTitle();
        string GetBlackboardSubTitle();
        IEnumerable<string> SectionNames { get; }
        IEnumerable<IVariableDeclarationModel> GetSectionRows(string sectionName);
    }
}
