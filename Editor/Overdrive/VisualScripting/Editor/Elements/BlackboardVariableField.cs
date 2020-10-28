using System;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEditor.GraphToolsFoundation.Overdrive.Bridge;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting
{
    /// <summary>
    /// A variable pill for the declaration in the blackboard.
    /// If a usage of this variable is renamed, the name will be updated by rebuilding the black board, no need for binding or manual update
    /// </summary>
    public class BlackboardVariableField : BlackboardField, IHighlightable, IRenamableGraphElement, IVisualScriptingField
    {
        TextField m_TitleTextfield;
        Label m_TitleLabel;

        public IGraphElementModel ExpandableGraphElementModel => null;

        public string TitleValue => VariableDeclarationModel.DisplayTitle.Nicify();

        public VisualElement TitleEditor => m_TitleTextfield ?? (m_TitleTextfield = new TextField { name = "titleEditor", isDelayed = true });
        public VisualElement TitleElement => this;

        public IVariableDeclarationModel VariableDeclarationModel => Model as IVariableDeclarationModel;

        public void Expand() {}
        public virtual bool CanInstantiateInGraph() => true;

        public override bool IsRenamable()
        {
            return VariableDeclarationModel is VariableDeclarationModel;
        }

        public bool Highlighted
        {
            get => highlighted;
            set => highlighted = value;
        }

        public bool IsFramable() => false;

        public bool EditTitleCancelled { get; set; } = false;

        public RenameDelegate RenameDelegate => OpenTextEditor;

        public BlackboardVariableField(Store store, IVariableDeclarationModel variableDeclarationModel, GraphView graphView)
        {
            SetupBuildAndUpdate(variableDeclarationModel, store, graphView);

            UpdateTitleFromModel();

            typeText = variableDeclarationModel.DataType.GetMetadata(variableDeclarationModel.GraphModel.Stencil).FriendlyName;

            icon = variableDeclarationModel.IsExposed
                ? GraphViewStaticBridge.LoadIconRequired("GraphView/Nodes/BlackboardFieldExposed.png")
                : null;

            var pill = this.MandatoryQ<Pill>("pill");
            pill.tooltip = TitleValue;

            pill.EnableInClassList("read-only", (variableDeclarationModel.Modifiers & ModifierFlags.ReadOnly) != 0);
            pill.EnableInClassList("write-only", (variableDeclarationModel.Modifiers & ModifierFlags.WriteOnly) != 0);

            viewDataKey = variableDeclarationModel.Guid.ToString() + "__" + Blackboard.k_PersistenceKey;
        }

        public override void OnSelected()
        {
            base.OnSelected();
            GraphView.HighlightGraphElements();
        }

        public override void OnUnselected()
        {
            base.OnUnselected();
            GraphView.ClearGraphElementsHighlight(ShouldHighlightItemUsage);
        }

        public bool ShouldHighlightItemUsage(IGraphElementModel model)
        {
            var variableModel = model as IVariableNodeModel;
            var candidate = model as IVariableDeclarationModel;
            return variableModel != null
                && Equals(variableModel.VariableDeclarationModel, VariableDeclarationModel)
                || Equals(candidate, VariableDeclarationModel);
        }

        public void UpdateTitleFromModel()
        {
            text = VariableDeclarationModel.DisplayTitle;
        }
    }
}
