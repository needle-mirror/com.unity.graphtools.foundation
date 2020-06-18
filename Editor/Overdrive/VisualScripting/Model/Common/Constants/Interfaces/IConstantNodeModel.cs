using System;
using System.Collections.Generic;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.GraphViewModel;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting
{
    public interface IConstantNodeModel : IHasMainOutputPort, IHasSingleOutputPort
    {
        object ObjectValue { get; }
        bool IsLocked { get; }
        Type Type { get; }
    }

    public interface IStringWrapperConstantModel : IConstantNodeModel
    {
        List<string> GetAllInputNames(IEditorDataModel editorDataModel);
        string StringValue { get; set; }
        string Label { get; }
    }
}
