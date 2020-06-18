using System;
using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.GraphViewModel;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests
{
    [Serializable]
    class TestNodeModel : NodeModel
    {
        protected override void OnDefineNode()
        {
            AddDataInputPort<float>("one");
        }
    }
}
