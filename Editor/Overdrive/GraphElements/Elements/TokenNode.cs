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
            if (model is IHasSingleInputPort inputPortHolder && inputPortHolder.GTFInputPort != null)
            {
                Debug.Assert(inputPortHolder.GTFInputPort.Direction == Direction.Input);
                return inputPortHolder.GTFInputPort;
            }

            return null;
        }

        static IGTFGraphElementModel ExtractOutputPortModel(IGTFGraphElementModel model)
        {
            if (model is IHasSingleOutputPort outputPortHolder && outputPortHolder.GTFOutputPort != null)
            {
                Debug.Assert(outputPortHolder.GTFOutputPort.Direction == Direction.Output);
                return outputPortHolder.GTFOutputPort;
            }

            return null;
        }
    }
}
