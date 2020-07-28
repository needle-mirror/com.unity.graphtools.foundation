using System;
using System.Collections.Generic;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.Plugins;
using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.GraphViewModel;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting
{
    public interface IEditorDataModel : IGTFEditorDataModel
    {
        GameObject BoundObject { get; set; }
    }
}
