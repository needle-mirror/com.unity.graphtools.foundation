using System;
using UnityEditor.VisualScripting.Editor.SmartSearch;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model.Stencils;
using UnityEngine;

namespace UnityEditor.VisualScripting.Model
{
    [SearcherItem(typeof(ClassStencil), SearcherContext.Graph, k_Title)]
    [Serializable]
    public class GetKeyNodeModel : BaseInputNodeModel
    {
        const string k_Title = "Get Key";

        public override string Title => k_Title;

        protected override string MethodName(IPortModel portModel)
        {
            switch (Mode)
            {
                case KeyDownEventModel.EventMode.Pressed:
                    return nameof(Input.GetKeyDown);
                case KeyDownEventModel.EventMode.Released:
                    return nameof(Input.GetKeyUp);
            }

            return nameof(Input.GetKey);
        }

        protected override void OnDefineNode()
        {
            base.OnDefineNode();
            InputPort = AddDataInputPort("Key Choice", defaultValue: KeyCode.Space);
            ButtonOutputPort = AddDataOutputPort<bool>(nameof(Input.GetKey));
        }
    }
}
