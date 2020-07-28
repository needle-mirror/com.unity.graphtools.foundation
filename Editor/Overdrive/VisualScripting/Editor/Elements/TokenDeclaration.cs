using System;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEditor.GraphToolsFoundation.Overdrive.GraphElements;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using UnityEditor.GraphToolsFoundation.Overdrive.Bridge;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UIElements;
using IDroppable = UnityEditor.GraphToolsFoundation.Overdrive.GraphElements.IDroppable;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting
{
    public class TokenDeclaration : GraphElement, IDroppable, IHighlightable, IRenamable
    {
        readonly Pill m_Pill;
        TextField m_TitleTextfield;

        public string TitleValue => Declaration.DisplayTitle;

        public VisualElement TitleEditor => m_TitleTextfield ?? (m_TitleTextfield = new TextField { name = "titleEditor", isDelayed = true });

        public bool EditTitleCancelled { get; set; } = false;

        public VisualElement TitleElement => this;
        public IGTFVariableDeclarationModel Declaration => Model as IGTFVariableDeclarationModel;

        public bool IsFramable() => true;

        public RenameDelegate RenameDelegate => null;

        public void SetClasses()
        {
            ClearClassList();
            AddToClassList("token");
            AddToClassList("declaration");

            AddToClassList(Declaration?.VariableType == VariableType.GraphVariable ? "graphVariable" : "functionVariable");
        }

        public TokenDeclaration(Store store, IGTFVariableDeclarationModel model, GraphView graphView)
        {
            m_Pill = new Pill();
            Add(m_Pill);

            styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>(UICreationHelper.templatePath + "Token.uss"));

            SetupBuildAndUpdate(model, store, graphView);

            m_Pill.icon = Declaration.IsExposed
                ? GraphViewStaticBridge.LoadIconRequired("GraphView/Nodes/BlackboardFieldExposed.png")
                : null;

            m_Pill.text = Declaration.DisplayTitle;

            var variableModel = model as VariableDeclarationModel;
            Stencil stencil = store.GetState().CurrentGraphModel?.Stencil;
            if (variableModel != null && stencil != null && variableModel.DataType.IsValid)
            {
                string friendlyTypeName = variableModel.DataType.GetMetadata(stencil).FriendlyName;
                Assert.IsTrue(!string.IsNullOrEmpty(friendlyTypeName));
                tooltip = $"{variableModel.VariableString} declaration of type {friendlyTypeName}";
                if (!string.IsNullOrEmpty(variableModel.Tooltip))
                    tooltip += "\n" + variableModel.Tooltip;
            }

            SetClasses();

            this.EnableRename();

            if (model != null)
                viewDataKey = model.Guid.ToString();
        }

        public override void OnUnselected()
        {
            base.OnUnselected();
            (GraphView as VseGraphView).ClearGraphElementsHighlight(ShouldHighlightItemUsage);
        }

        public bool ShouldHighlightItemUsage(IGTFGraphElementModel model)
        {
            switch (model)
            {
                case IGTFVariableNodeModel variableModel
                    when ReferenceEquals(variableModel.VariableDeclarationModel, Declaration):
                case IGTFVariableDeclarationModel variableDeclarationModel
                    when ReferenceEquals(variableDeclarationModel, Declaration):
                    return true;
            }

            return false;
        }

        public override bool IsSelected(VisualElement selectionContainer)
        {
            var gView = selectionContainer?.GetFirstOfType<GraphView>();
            return gView != null && gView.Selection.Contains(this);
        }

        public int FindIndexInParent()
        {
            // Find index of child so we can provide with the correct information
            for (int i = 0; i < parent?.childCount; i++)
            {
                if (this == parent.ElementAt(i))
                    return i;
            }
            return -1;
        }

        public TokenDeclaration Clone()
        {
            var clone = new TokenDeclaration(Store, Declaration, GraphView)
            {
                viewDataKey = Guid.NewGuid().ToString()
            };
            return clone;
        }

        public bool Highlighted
        {
            get => m_Pill.highlighted;
            set => m_Pill.highlighted = value;
        }
    }
}
