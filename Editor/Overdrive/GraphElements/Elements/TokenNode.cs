using System;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public class TokenNode : Node
    {
        public static readonly string k_TokenModifierUssClassName = k_UssClassName.WithUssModifier("token");
        public static readonly string k_ConstantModifierUssClassName = k_UssClassName.WithUssModifier("constant-token");
        public static readonly string k_VariableModifierUssClassName = k_UssClassName.WithUssModifier("variable-token");
        public static readonly string k_PortalModifierUssClassName = k_UssClassName.WithUssModifier("portal");
        public static readonly string k_PortalEntryModifierUssClassName = k_UssClassName.WithUssModifier("portal-entry");
        public static readonly string k_PortalExitModifierUssClassName = k_UssClassName.WithUssModifier("portal-exit");

        public static readonly string k_TitleIconContainerPartName = "title-icon-container";
        public static readonly string k_ConstantEditorPartName = "constant-editor";
        public static readonly string k_InputPortContainerPartName = "inputs";
        public static readonly string k_OutputPortContainerPartName = "outputs";

        protected override void BuildPartList()
        {
            PartList.AppendPart(SinglePortContainerPart.Create(k_InputPortContainerPartName, ExtractInputPortModel(Model), this, k_UssClassName));
            PartList.AppendPart(IconTitleProgressPart.Create(k_TitleIconContainerPartName, Model, this, k_UssClassName));
            PartList.AppendPart(ConstantNodeEditorPart.Create(k_ConstantEditorPartName, Model, this, k_UssClassName, Store?.GetState().EditorDataModel));
            PartList.AppendPart(SinglePortContainerPart.Create(k_OutputPortContainerPartName, ExtractOutputPortModel(Model), this, k_UssClassName));
        }

        protected override void PostBuildUI()
        {
            base.PostBuildUI();

            AddToClassList(k_TokenModifierUssClassName);
            this.AddStylesheet("TokenNode.uss");

            switch (Model)
            {
                case IEdgePortalEntryModel _:
                    AddToClassList(k_PortalModifierUssClassName);
                    AddToClassList(k_PortalEntryModifierUssClassName);
                    break;
                case IEdgePortalExitModel _:
                    AddToClassList(k_PortalModifierUssClassName);
                    AddToClassList(k_PortalExitModifierUssClassName);
                    break;
                case ConstantNodeModel _:
                    AddToClassList(k_ConstantModifierUssClassName);
                    break;
                case VariableNodeModel _:
                    AddToClassList(k_VariableModifierUssClassName);
                    break;
            }
        }

        static IGraphElementModel ExtractInputPortModel(IGraphElementModel model)
        {
            if (model is ISingleInputPortNode inputPortHolder && inputPortHolder.InputPort != null)
            {
                Debug.Assert(inputPortHolder.InputPort.Direction == Direction.Input);
                return inputPortHolder.InputPort;
            }

            return null;
        }

        static IGraphElementModel ExtractOutputPortModel(IGraphElementModel model)
        {
            if (model is ISingleOutputPortNode outputPortHolder && outputPortHolder.OutputPort != null)
            {
                Debug.Assert(outputPortHolder.OutputPort.Direction == Direction.Output);
                return outputPortHolder.OutputPort;
            }

            return null;
        }

        public override bool IsRenamable()
        {
            if (!base.IsRenamable())
                return false;

            if (NodeModel is IRenamable)
                return true;

            var declarationModel = (NodeModel as IVariableNodeModel)?.VariableDeclarationModel;
            return declarationModel is IRenamable;
        }

        public override bool ShouldHighlightItemUsage(IGraphElementModel elementModel)
        {
            var currentVariableModel = NodeModel as IVariableNodeModel;
            var currentEdgePortalModel = Model as IEdgePortalModel;
            if (currentVariableModel?.VariableDeclarationModel == null && currentEdgePortalModel == null)
                return false;

            switch (elementModel)
            {
                case IVariableNodeModel variableModel
                    when ReferenceEquals(variableModel.VariableDeclarationModel, currentVariableModel?.VariableDeclarationModel):
                case IVariableDeclarationModel variableDeclarationModel
                    when ReferenceEquals(variableDeclarationModel, currentVariableModel?.VariableDeclarationModel):
                case IEdgePortalModel edgePortalModel
                    when ReferenceEquals(edgePortalModel.DeclarationModel, currentEdgePortalModel?.DeclarationModel):
                    return true;
            }

            return false;
        }
    }
}
