using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using UnityEditor.VisualScripting.Editor.SmartSearch;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model.Stencils;
using UnityEditor.VisualScripting.Model.Translators;
using UnityEngine;

namespace UnityEditor.VisualScripting.Model
{
    [SearcherItem(typeof(ClassStencil), SearcherContext.Graph, k_Title)]
    [Serializable]
    public class GetInputNodeModel : BaseInputNodeModel
    {
        const string k_Title = "Get Input";

        InputName m_InputName;

        protected override string MethodName(IPortModel portModel)
        {
            if (portModel.Name == nameof(Input.GetAxis))
                return portModel.Name;
            switch (Mode)
            {
                case KeyDownEventModel.EventMode.Pressed:
                    return nameof(Input.GetButtonDown);
                case KeyDownEventModel.EventMode.Released:
                    return nameof(Input.GetButtonUp);
            }

            return nameof(Input.GetButton);
        }

        public override string Title => k_Title;

        public IPortModel AxisOutputPort { get; private set; }

        protected override void OnDefineNode()
        {
            base.OnDefineNode();
            InputPort = AddDataInputPort<InputName>("Input Choice");
            ButtonOutputPort = AddDataOutputPort<bool>(nameof(Input.GetButton));
            AxisOutputPort = AddDataOutputPort<float>(nameof(Input.GetAxis));
        }
    }
}
