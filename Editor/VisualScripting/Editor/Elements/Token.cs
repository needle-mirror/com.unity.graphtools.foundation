using System;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEditor.VisualScripting.Editor.Highlighting;
using UnityEditor.VisualScripting.Editor.Renamable;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.VisualScripting.Editor.ConstantEditor;
using UnityEditor.VisualScripting.Model.Stencils;

namespace UnityEditor.VisualScripting.Editor
{
    public class Token : TokenNode, IHighlightable, ICustomColor, IBadgeContainer, IRenamable, IMovable, INodeState
    {
        INodeModel Model { get; }
        public Store Store { get; }

        readonly GraphView m_GraphView;
        SerializedObject m_SerializedObject;

        TextField m_TitleTextfield;
        Label m_TitleLabel;

        public string TitleValue => Model.Title.Nicify();

        public VisualElement TitleEditor => (Model is ConstantNodeModel) ? TokenEditor : m_TitleTextfield ?? (m_TitleTextfield = new TextField { name = "titleEditor", isDelayed = true });
        public VisualElement TitleElement => this;

        public IGraphElementModel GraphElementModel => Model;

        VisualElement TokenEditor { get; set; }

        public bool Highlighted
        {
            get => highlighted;
            set => highlighted = value;
        }

        public override bool IsRenamable()
        {
            if (!base.IsRenamable())
                return false;

            if (Model.Capabilities.HasFlag(CapabilityFlags.Renamable))
                return true;

            IVariableDeclarationModel declarationModel = (Model as IVariableModel)?.DeclarationModel;
            return declarationModel != null && declarationModel.Capabilities.HasFlag(CapabilityFlags.Renamable);
        }

        public bool IsFramable() => true;

        public bool EditTitleCancelled { get; set; } = false;

        public RenameDelegate RenameDelegate => null;

        internal static TypeHandle[] s_PropsToHideLabel =
        {
            TypeHandle.Int,
            TypeHandle.Float,
            TypeHandle.Vector2,
            TypeHandle.Vector3,
            TypeHandle.Vector4,
            TypeHandle.String
        };

        bool TokenEditorNeedsLabel
        {
            get
            {
                if (Model is ConstantNodeModel constantNodeModel)
                    return !s_PropsToHideLabel.Contains(constantNodeModel.Type.GenerateTypeHandle(Model.GraphModel.Stencil));
                return true;
            }
        }

        public Token(INodeModel model, Store store, Port input, Port output, GraphView graphView, Texture2D icon = null) : base(input, output)
        {
            Store = store;
            m_GraphView = graphView;
            Model = model;
            this.icon = icon;

            styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>(UICreationHelper.templatePath + "PropertyField.uss"));
            styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>(UICreationHelper.templatePath + "Token.uss"));

            base.SetPosition(new Rect(model.Position, Vector2.zero));

            capabilities = VseUtility.ConvertCapabilities(model);

            if (model is IObjectReference modelReference)
            {
                if (modelReference is IExposeTitleProperty titleProperty)
                {
                    m_TitleLabel = this.Q<Label>("title-label");
                    m_TitleLabel.bindingPath = titleProperty.TitlePropertyName;
                }
            }

            if (model is ConstantNodeModel constantNodeModel)
                SetupConstantEditor(constantNodeModel);
            else
                base.title = model.Title;

            if (model is IVariableModel variableModel && variableModel.DeclarationModel != null)
            {
                switch (variableModel.DeclarationModel.Modifiers)
                {
                    case ModifierFlags.ReadOnly:
                        AddToClassList("read-only");
                        break;
                    case ModifierFlags.WriteOnly:
                        AddToClassList("write-only");
                        break;
                }
            }

            this.EnableRename();

            var nodeModel = model as NodeModel;
            if (nodeModel != null)
            {
                tooltip = $"{nodeModel.VariableString}";
                if (!string.IsNullOrEmpty(nodeModel.DataTypeString))
                    tooltip += $" of type {nodeModel.DataTypeString}";
                if (model is IVariableModel currentVariableModel &&
                    !string.IsNullOrEmpty(currentVariableModel.DeclarationModel?.Tooltip))
                    tooltip += "\n" + currentVariableModel.DeclarationModel.Tooltip;
            }

            viewDataKey = model.GetId();
        }

        void SetupConstantEditor(ConstantNodeModel constantNodeModel)
        {
            EnableInClassList("constant", true);

            void OnValueChanged(IChangeEvent evt)
            {
                if (constantNodeModel.OutputPort.Connected)
                    Store.Dispatch(new RefreshUIAction(UpdateFlags.RequestCompilation));
                // TODO: ???
                if (TokenEditorNeedsLabel)
                    title = Model.Title;
            }

            TokenEditor = this.CreateEditorForNodeModel((IConstantNodeModel)Model, OnValueChanged);

            var label = this.MandatoryQ<Label>("title-label");

            if (!TokenEditorNeedsLabel)
                label.style.width = 0;
            label.parent.Insert(0, TokenEditor);
            if (Model is IStringWrapperConstantModel icm)
                label.parent.Insert(0, new Label(icm.Label));
        }

        public void UpdatePinning()
        {
        }

        public bool NeedStoreDispatch { get; set; } = true;

        public override void OnUnselected()
        {
            base.OnUnselected();
            ((VseGraphView)m_GraphView).ClearGraphElementsHighlight(ShouldHighlightItemUsage);
        }

        public bool ShouldHighlightItemUsage(IGraphElementModel elementModel)
        {
            var currentVariableModel = Model as IVariableModel;
            // 'this' tokens have a null declaration model
            if (currentVariableModel?.DeclarationModel == null)
                return (Model is ThisNodeModel && elementModel is ThisNodeModel);

            switch (elementModel)
            {
                case IVariableModel variableModel
                    when ReferenceEquals(variableModel.DeclarationModel, currentVariableModel.DeclarationModel):
                case IVariableDeclarationModel variableDeclarationModel
                    when ReferenceEquals(variableDeclarationModel, currentVariableModel.DeclarationModel):
                    return true;
            }

            return false;
        }

        public void ResetColor()
        {
            var border = this.MandatoryQ("node-border");
            border.style.backgroundColor = StyleKeyword.Null;
            border.style.backgroundImage = StyleKeyword.Null;
        }

        public void SetColor(Color c)
        {
            var border = this.MandatoryQ("node-border");
            border.style.backgroundColor = c;
            border.style.backgroundImage = null;
        }

        public IconBadge ErrorBadge { get; set; }
        public ValueBadge ValueBadge { get; set; }
        public NodeUIState UIState { get; set; }
    }
}
