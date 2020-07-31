using System;
using UnityEditor.GraphToolsFoundation.Overdrive.Bridge;
using UnityEditor.GraphToolsFoundation.Overdrive.GraphElements;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.GraphViewModel;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting
{
    public class Token : TokenNode, IHighlightable
    {
        SerializedObject m_SerializedObject;

        public override bool IsRenamable()
        {
            if (!base.IsRenamable())
                return false;

            if (NodeModel is Overdrive.Model.IRenamable)
                return true;

            var declarationModel = (NodeModel as IGTFVariableNodeModel)?.VariableDeclarationModel;
            return declarationModel is Overdrive.Model.IRenamable;
        }

        public static readonly string k_TitleIconContainerPartName = "title-icon-container";
        public static readonly string k_ConstantEditorPartName = "constant-editor";

        public static readonly string k_ConstantModifierUssClassName = k_UssClassName.WithUssModifier("constant-token");
        public static readonly string k_VariableModifierUssClassName = k_UssClassName.WithUssModifier("variable-token");
        public static readonly string k_ReadOnlyModifierUssClassName = k_UssClassName.WithUssModifier("read-only");
        public static readonly string k_WriteOnlyModifierUssClassName = k_UssClassName.WithUssModifier("write-only");
        public static readonly string k_PortalModifierUssClassName = k_UssClassName.WithUssModifier("portal");
        public static readonly string k_PortalEntryModifierUssClassName = k_UssClassName.WithUssModifier("portal-entry");
        public static readonly string k_PortalExitModifierUssClassName = k_UssClassName.WithUssModifier("portal-exit");

        protected override void BuildPartList()
        {
            base.BuildPartList();

            PartList.ReplacePart(k_TitleContainerPartName, IconTitleProgressPart.Create(k_TitleIconContainerPartName, Model, this, k_UssClassName));
            PartList.InsertPartAfter(k_TitleIconContainerPartName, ConstantNodeEditorPart.Create(k_ConstantEditorPartName, Model, this, k_UssClassName, Store.GetState().EditorDataModel));
        }

        protected override void BuildElementUI()
        {
            base.BuildElementUI();

            if (Model is ConstantNodeModel)
            {
                AddToClassList(k_ConstantModifierUssClassName);
            }
            else if (Model is VariableNodeModel)
            {
                AddToClassList(k_VariableModifierUssClassName);
            }
        }

        protected override void PostBuildUI()
        {
            base.PostBuildUI();

            styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>(UICreationHelper.templatePath + "Node.uss"));
            styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>(UICreationHelper.templatePath + "Token.uss"));
        }

        protected override void UpdateElementFromModel()
        {
            base.UpdateElementFromModel();

            if (Model is IGTFVariableNodeModel variableModel && variableModel.VariableDeclarationModel != null)
            {
                switch (variableModel.VariableDeclarationModel.Modifiers)
                {
                    case ModifierFlags.ReadOnly:
                        AddToClassList(k_ReadOnlyModifierUssClassName);
                        break;
                    case ModifierFlags.WriteOnly:
                        AddToClassList(k_WriteOnlyModifierUssClassName);
                        break;
                }
            }
            else if (Model is IGTFEdgePortalEntryModel)
            {
                AddToClassList(k_PortalModifierUssClassName);
                AddToClassList(k_PortalEntryModifierUssClassName);
            }
            else if (Model is IGTFEdgePortalExitModel)
            {
                AddToClassList(k_PortalModifierUssClassName);
                AddToClassList(k_PortalExitModifierUssClassName);
            }

            if (Model is NodeModel nodeModel)
            {
                tooltip = $"{nodeModel.VariableString}";
                if (!string.IsNullOrEmpty(nodeModel.DataTypeString))
                    tooltip += $" of type {nodeModel.DataTypeString}";
                if (Model is IGTFVariableNodeModel currentVariableModel &&
                    !string.IsNullOrEmpty(currentVariableModel.VariableDeclarationModel?.Tooltip))
                    tooltip += "\n" + currentVariableModel.VariableDeclarationModel.Tooltip;

                if (nodeModel.HasUserColor)
                {
                    var border = this.MandatoryQ(SelectionBorder.k_ContentContainerElementName);
                    border.style.backgroundColor = nodeModel.Color;
                    border.style.backgroundImage = null;
                }
                else
                {
                    var border = this.MandatoryQ(SelectionBorder.k_ContentContainerElementName);
                    border.style.backgroundColor = StyleKeyword.Null;
                    border.style.backgroundImage = StyleKeyword.Null;
                }
            }
        }

        bool m_IsMovable = true;
        public override bool IsMovable => m_IsMovable;

        public void SetMovable(bool movable)
        {
            m_IsMovable = movable;
        }

        public override void OnUnselected()
        {
            base.OnUnselected();
            ((VseGraphView)GraphView).ClearGraphElementsHighlight(ShouldHighlightItemUsage);
        }

        public new bool ShouldHighlightItemUsage(IGTFGraphElementModel elementModel)
        {
            var currentVariableModel = NodeModel as IGTFVariableNodeModel;
            var currentEdgePortalModel = Model as IGTFEdgePortalModel;
            // 'this' tokens have a null declaration model
            if (currentVariableModel?.VariableDeclarationModel == null && currentEdgePortalModel == null)
                return NodeModel is ThisNodeModel && elementModel is ThisNodeModel;

            switch (elementModel)
            {
                case IGTFVariableNodeModel variableModel
                    when ReferenceEquals(variableModel.VariableDeclarationModel, currentVariableModel?.VariableDeclarationModel):
                case IVariableDeclarationModel variableDeclarationModel
                    when ReferenceEquals(variableDeclarationModel, currentVariableModel?.VariableDeclarationModel):
                case IGTFEdgePortalModel edgePortalModel
                    when ReferenceEquals(edgePortalModel.DeclarationModel, currentEdgePortalModel?.DeclarationModel):
                    return true;
            }

            return false;
        }
    }
}
