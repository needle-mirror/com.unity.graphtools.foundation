using System;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEditor.VisualScripting.Editor.Highlighting;
using UnityEditor.VisualScripting.Editor.Renamable;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model;
using UnityEditor.VisualScripting.Model.Stencils;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UIElements;

namespace UnityEditor.VisualScripting.Editor
{
    public class TokenDeclaration : GraphElement, IDroppable, IHighlightable, IRenamable, IMovable
    {
        readonly Store m_Store;
        readonly GraphView m_GraphView;
        readonly Pill m_Pill;
        TextField m_TitleTextfield;

        Label m_TitleLabel;

        public Store Store => m_Store;
        public string TitleValue => Declaration.Title.Nicify();

        public VisualElement TitleEditor => m_TitleTextfield ?? (m_TitleTextfield = new TextField { name = "titleEditor", isDelayed = true });

        public bool EditTitleCancelled { get; set; } = false;

        public VisualElement TitleElement => this;
        public IVariableDeclarationModel Declaration { get; }

        public override bool IsRenamable()
        {
            if (!base.IsRenamable())
                return false;

            if (!IsFunctionParameter)
                return true;

            var variableDeclarationModel = Declaration as VariableDeclarationModel;
            return (variableDeclarationModel != null && variableDeclarationModel.Capabilities.HasFlag(CapabilityFlags.Renamable));
        }

        public bool IsFramable() => true;

        public RenameDelegate RenameDelegate => null;

        bool IsFunctionParameter => (Declaration != null) && Declaration.VariableType == VariableType.FunctionParameter;

        public void SetClasses()
        {
            ClearClassList();
            AddToClassList("token");
            AddToClassList("declaration");

            AddToClassList(IsFunctionParameter
                ? "functionParameter"
                : (Declaration?.VariableType == VariableType.GraphVariable ? "graphVariable" : "functionVariable"));
        }

        public TokenDeclaration(Store store, IVariableDeclarationModel model, GraphView graphView)
        {
            m_Pill = new Pill();
            Add(m_Pill);

            if (model is IObjectReference modelReference)
            {
                if (modelReference is IExposeTitleProperty titleProperty)
                {
                    m_TitleLabel = m_Pill.Q<Label>("title-label");
                    m_TitleLabel.bindingPath = titleProperty.TitlePropertyName;
                }
            }

            styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>(UICreationHelper.templatePath + "Token.uss"));

            Declaration = model;
            m_Store = store;
            m_GraphView = graphView;

            m_Pill.icon = Declaration.IsExposed
                ? VisualScriptingIconUtility.LoadIconRequired("GraphView/Nodes/BlackboardFieldExposed.png")
                : null;

            m_Pill.text = Declaration.Title;

            if (model != null)
                capabilities = VseUtility.ConvertCapabilities(model);

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
                viewDataKey = model.GetId();
        }

        public override void OnUnselected()
        {
            base.OnUnselected();
            ((VseGraphView)m_GraphView).ClearGraphElementsHighlight(ShouldHighlightItemUsage);
        }

        public bool ShouldHighlightItemUsage(IGraphElementModel model)
        {
            switch (model)
            {
                case IVariableModel variableModel
                    when ReferenceEquals(variableModel.DeclarationModel, Declaration):
                case IVariableDeclarationModel variableDeclarationModel
                    when ReferenceEquals(variableDeclarationModel, Declaration):
                    return true;
            }

            return false;
        }

        public override bool IsSelected(VisualElement selectionContainer)
        {
            var gView = selectionContainer?.GetFirstOfType<GraphView>();
            return gView != null && gView.selection.Contains(this);
        }

        public override void SetPosition(Rect newPos)
        {
            style.position = Position.Absolute;
            style.left = newPos.x;
            style.top = newPos.y;
        }

        public void UpdatePinning()
        {
        }

        public bool NeedStoreDispatch => false;

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
            var clone = new TokenDeclaration(m_Store, Declaration, m_GraphView)
            {
                viewDataKey = Guid.NewGuid().ToString()
            };
            if (Declaration is LoopVariableDeclarationModel loopVariableDeclarationModel)
                VseUtility.AddTokenIcon(clone, loopVariableDeclarationModel.TitleComponentIcon);
            return clone;
        }

        public IGraphElementModel GraphElementModel => Declaration;

        public bool Highlighted
        {
            get => m_Pill.highlighted;
            set => m_Pill.highlighted = value;
        }
    }
}
