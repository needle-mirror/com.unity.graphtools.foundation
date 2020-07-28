using System;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.GraphElements
{
    public class TokenNode : Node
    {
        public static readonly string k_TokenModifierUssClassName = k_UssClassName.WithUssModifier("token");

        public static readonly string k_InputPortContainerPartName = "inputs";
        public static readonly string k_OutputPortContainerPartName = "outputs";

        protected override void BuildPartList()
        {
            PartList.AppendPart(SinglePortContainerPart.Create(k_InputPortContainerPartName, ExtractInputPortModel(Model), this, k_UssClassName));
            PartList.AppendPart(EditableTitlePart.Create(k_TitleContainerPartName, Model, this, k_UssClassName));
            PartList.AppendPart(SinglePortContainerPart.Create(k_OutputPortContainerPartName, ExtractOutputPortModel(Model), this, k_UssClassName));
        }

        protected override void PostBuildUI()
        {
            base.PostBuildUI();

            AddToClassList(k_TokenModifierUssClassName);
            this.AddStylesheet("TokenNode.uss");
        }

        static IGTFGraphElementModel ExtractInputPortModel(IGTFGraphElementModel model)
        {
            if (model is ISingleInputPortNode inputPortHolder && inputPortHolder.InputPort != null)
            {
                Debug.Assert(inputPortHolder.InputPort.Direction == Direction.Input);
                return inputPortHolder.InputPort;
            }

            return null;
        }

        static IGTFGraphElementModel ExtractOutputPortModel(IGTFGraphElementModel model)
        {
            if (model is ISingleOutputPortNode outputPortHolder && outputPortHolder.OutputPort != null)
            {
                Debug.Assert(outputPortHolder.OutputPort.Direction == Direction.Output);
                return outputPortHolder.OutputPort;
            }

            return null;
        }
    }
}
