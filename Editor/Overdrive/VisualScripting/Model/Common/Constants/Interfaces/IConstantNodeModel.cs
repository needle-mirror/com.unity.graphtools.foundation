using System;
using System.Collections.Generic;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.GraphViewModel;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting
{
    public interface IConstantNodeModel : IGTFConstantNodeModel, IHasMainOutputPort
    {
    }

    public interface IStringWrapperConstantModel : IConstant
    {
        List<string> GetAllInputNames(IEditorDataModel editorDataModel);
        string StringValue { get; set; }
        string Label { get; }
    }
}
