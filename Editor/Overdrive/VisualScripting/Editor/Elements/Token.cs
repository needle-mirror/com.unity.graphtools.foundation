using System;
using UnityEditor.GraphToolsFoundation.Overdrive.GraphElements;
using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.Highlighting;
using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.GraphViewModel;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting
{
    public class Token : TokenNode, IHighlightable, IBadgeContainer, INodeState
    {
        public new INodeModel NodeModel => base.NodeModel as INodeModel;
        public new Store Store => base.Store as Store;

        SerializedObject m_SerializedObject;

        public IGraphElementModel GraphElementModel => NodeModel;

        public override bool IsRenamable()
        {
            if (!base.IsRenamable())
                return false;

            if (NodeModel is Overdrive.Model.IRenamable)
                return true;

            IVariableDeclarationModel declarationModel = (NodeModel as IVariableModel)?.DeclarationModel;
            return declarationModel is Overdrive.Model.IRenamable;
        }

        VisualElement m_ContentContainer;
        public override VisualElement contentContainer => m_ContentContainer ?? this;

        public static readonly string k_TitleIconContainerPartName = "title-icon-container";
        public static readonly string k_ConstantEditorPartName = "constant-editor";

        public static readonly string k_SelectionBorderElementName = "selection-border";
        public static readonly string k_ContentContainerElementName = "content-container";

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
            m_ContentContainer = this.AddBorder(k_UssClassName);
            base.BuildElementUI();

            if (Model is ConstantNodeModel)
            {
                AddToClassList(k_ConstantModifierUssClassName);
            }
            else if (Model is VariableNodeModel)
            {
                AddToClassList(k_VariableModifierUssClassName);
            }

            viewDataKey = NodeModel.GetId();

            this.AddOverlay();
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

            if (Model is IVariableModel variableModel && variableModel.DeclarationModel != null)
            {
                switch (variableModel.DeclarationModel.Modifiers)
                {
                    case ModifierFlags.ReadOnly:
                        AddToClassList(k_ReadOnlyModifierUssClassName);
                        break;
                    case ModifierFlags.WriteOnly:
                        AddToClassList(k_WriteOnlyModifierUssClassName);
                        break;
                }
            }
            else if (Model is IEdgePortalEntryModel)
            {
                AddToClassList(k_PortalModifierUssClassName);
                AddToClassList(k_PortalEntryModifierUssClassName);
            }
            else if (Model is IEdgePortalExitModel)
            {
                AddToClassList(k_PortalModifierUssClassName);
                AddToClassList(k_PortalExitModifierUssClassName);
            }

            if (Model is NodeModel nodeModel)
            {
                tooltip = $"{nodeModel.VariableString}";
                if (!string.IsNullOrEmpty(nodeModel.DataTypeString))
                    tooltip += $" of type {nodeModel.DataTypeString}";
                if (Model is IVariableModel currentVariableModel &&
                    !string.IsNullOrEmpty(currentVariableModel.DeclarationModel?.Tooltip))
                    tooltip += "\n" + currentVariableModel.DeclarationModel.Tooltip;

                if (nodeModel.HasUserColor)
                {
                    var border = this.MandatoryQ(k_ContentContainerElementName);
                    border.style.backgroundColor = nodeModel.Color;
                    border.style.backgroundImage = null;
                }
                else
                {
                    var border = this.MandatoryQ(k_ContentContainerElementName);
                    border.style.backgroundColor = StyleKeyword.Null;
                    border.style.backgroundImage = StyleKeyword.Null;
                }
            }

            UIState = NodeModel.State == ModelState.Disabled ? NodeUIState.Disabled : NodeUIState.Enabled;
            this.ApplyNodeState();
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

        public bool Highlighted
        {
            get => ClassListContains("highlighted");
            set => EnableInClassList("highlighted", value);
        }

        public bool ShouldHighlightItemUsage(IGraphElementModel elementModel)
        {
            var currentVariableModel = NodeModel as IVariableModel;
            var currentEdgePortalModel = Model as IEdgePortalModel;
            // 'this' tokens have a null declaration model
            if (currentVariableModel?.DeclarationModel == null && currentEdgePortalModel == null)
                return NodeModel is ThisNodeModel && elementModel is ThisNodeModel;

            switch (elementModel)
            {
                case IVariableModel variableModel
                    when ReferenceEquals(variableModel.DeclarationModel, currentVariableModel?.DeclarationModel):
                case IVariableDeclarationModel variableDeclarationModel
                    when ReferenceEquals(variableDeclarationModel, currentVariableModel?.DeclarationModel):
                case IEdgePortalModel edgePortalModel
                    when ReferenceEquals(edgePortalModel.DeclarationModel, currentEdgePortalModel?.DeclarationModel):
                    return true;
            }

            return false;
        }

        public IconBadge ErrorBadge { get; set; }
        public ValueBadge ValueBadge { get; set; }
        public NodeUIState UIState { get; set; }
    }
}
