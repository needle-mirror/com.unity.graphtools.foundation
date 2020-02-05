using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEditor.VisualScripting.Editor.Renamable;
using UnityEditor.VisualScripting.Editor.SmartSearch;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model;
using UnityEditor.VisualScripting.Model.Services;
using UnityEditor.VisualScripting.Model.Stencils;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.VisualScripting.Editor
{
    public class FunctionNode : StackNode, IRenamable
    {
        internal enum SupportedFields
        {
            Variable,
            Parameter
        }

        public const string AddButtonName = "functionHeaderNewVariableButton";

        const float k_TokenHeight = 28;

        protected internal readonly IFunctionModel m_FunctionModel;
        VisualElement VariableContainer { get; }
        readonly Button m_NewVariableButton;

        internal IEnumerable<TokenDeclaration> Variables => VariableContainer.Children().OfType<TokenDeclaration>();

        readonly VisualElement m_TitleElement;

        static GUIContent[] s_Options;
        static GUIContent[] s_EventOptions;

        public Store Store => m_Store;
        TextField m_TitleTextfield;

        Label m_TitleLabel;

        public override string title
        {
            get => m_TitleLabel.text.Nicify();
            set => m_TitleLabel.text = value;
        }

        public VisualElement TitleEditor => m_TitleTextfield ?? (m_TitleTextfield = new TextField { name = "titleEditor", isDelayed = true });
        public VisualElement TitleElement => m_TitleElement;

        public override bool IsRenamable() => base.IsRenamable() && m_FunctionModel.AllowChangesToModel;
        public bool IsFramable() => true;

        public bool EditTitleCancelled { get; set; } = false;

        public RenameDelegate RenameDelegate => null;

        public string TitleValue => m_FunctionModel.Title;

        public FunctionNode(Store store, IFunctionModel functionModel, INodeBuilder nodeBuilder)
            : base(store, functionModel, nodeBuilder)
        {
            m_FunctionModel = functionModel;

            styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>(UICreationHelper.templatePath + "FunctionNode.uss"));

            AddToClassList("function");
            if (functionModel is LoopStackModel ||
                functionModel is EventFunctionModel)
                AddToClassList("loopHeader");

            EnableInClassList("event", functionModel is EventFunctionModel);

            UICreationHelper.CreateFromTemplateAndStyle(headerContainer, "FunctionHeader");

            VariableContainer = this.MandatoryQ("functionHeaderVariableContainer");
            m_TitleElement = this.MandatoryQ("functionHeaderTitle");
            m_TitleElement.pickingMode = PickingMode.Ignore;

            var nodeIcon = this.MandatoryQ("functionHeaderIcon");
            nodeIcon.AddToClassList(((NodeModel)functionModel).IconTypeString);

            this.EnableRename();

            m_TitleLabel = this.Q<Label>(name: "titleLabel");

            if (functionModel is LoopStackModel loopStackModel)
            {
                var titleComponents = loopStackModel.BuildTitle();

                if (titleComponents.Any())
                {
                    VisualElement titleContainerElement = new VisualElement {name = "titleContainerElement"};
                    titleContainerElement.style.flexDirection = FlexDirection.Row;

                    foreach (var titleComponent in titleComponents)
                    {
                        if (titleComponent.titleComponentType == LoopStackModel.TitleComponentType.String)
                        {
                            titleContainerElement.Add(new Label
                                {name = "titleLabel", text = (string)titleComponent.titleObject});
                        }
                        else if (titleComponent.titleComponentType == LoopStackModel.TitleComponentType.Token)
                        {
                            if (titleComponent.titleObject == null)
                                continue;
                            var tokenDeclaration = new TokenDeclaration(store,
                                (IVariableDeclarationModel)titleComponent.titleObject,
                                m_GraphView);
                            VseUtility.AddTokenIcon(tokenDeclaration, titleComponent.titleComponentIcon);

                            titleContainerElement.Add(tokenDeclaration);
                        }
                    }
                    m_TitleElement.Add(titleContainerElement);
                }
            }
            else
            {
                m_TitleLabel.text = functionModel.Title;

                if (m_FunctionModel.HasReturnType)
                {
                    var returnTypeButton = new Button() { text = m_FunctionModel.ReturnType.GetMetadata(m_FunctionModel.GraphModel.Stencil).FriendlyName };
                    returnTypeButton.clickable.clickedWithEventInfo += e =>
                    {
                        SearcherService.ShowTypes(functionModel.GraphModel.Stencil, e.originalMousePosition, (type, i) =>
                        {
                            m_Store.Dispatch(new UpdateFunctionReturnTypeAction(m_FunctionModel, type));
                        });
                    };
                    m_TitleElement.Add(returnTypeButton);
                }
            }

            m_TitleElement.AddToClassList("functionHeaderTitle"); // TODO check parent and style

            m_NewVariableButton = (Button)this.MandatoryQ(AddButtonName);
            m_NewVariableButton.tooltip = functionModel.AllowChangesToModel ? "Add function parameters/variables" : "Add function variables";
            m_NewVariableButton.clickable.clicked += OnNewVariableButtonClicked;

            BuildVariables();

            //<HACK>
            // Case 972336
            // Should be reverted when YOGA is used instead of FlexBox
            RegisterCallback<GeometryChangedEvent>(e =>
            {
                bool emptyVariableContainer = VariableContainer.childCount == 0;

                VariableContainer.EnableInClassList("empty", emptyVariableContainer);

                if (emptyVariableContainer)
                    return;

                float y = 0;
                float height = k_TokenHeight;
                foreach (var token in VariableContainer.Children())
                {
                    y = Mathf.Max(y, token.layout.y);
                    height = Mathf.Max(height, token.layout.height + token.resolvedStyle.paddingBottom);
                }

                VariableContainer.style.height = y + height + VariableContainer.resolvedStyle.paddingBottom;
            });
            //</HACK>

            IGraphElementModel elementModelToRename = m_Store.GetState().EditorDataModel.ElementModelToRename;
            if (!ReferenceEquals(elementModelToRename, functionModel))
                return;

            if (m_GraphView is VseGraphView vseGraphView)
                vseGraphView.UIController.ElementToRename = this;
        }

        void BuildVariables()
        {
            foreach (IPortModel inputPortModel in m_FunctionModel.InputsByDisplayOrder)
            {
                if (inputPortModel.PortType != PortType.Instance && inputPortModel.PortType != PortType.Data)
                    continue;

                Image existingIcon = null;
                if (inputPortModel.PortType == PortType.Instance)
                {
                    AddToClassList("instance");
                    existingIcon = this.MandatoryQ<Image>("functionHeaderIcon");
                }

                VisualElement loopPortContainer = this.MandatoryQ("loopPortContainer");
                Port.CreateInputPort(m_Store, inputPortModel, loopPortContainer, VariableContainer, existingIcon);
            }

            foreach (var outputPortModel in m_FunctionModel.OutputsByDisplayOrder)
            {
                if (outputPortModel.PortType != PortType.Instance && outputPortModel.PortType != PortType.Data)
                    continue;

                var port = Port.Create(outputPortModel, m_Store, Orientation.Horizontal);
                VariableContainer.Add(port);
            }

            foreach (var local in m_FunctionModel.FunctionParameterModels)
            {
                if (!(local.SerializableAsset) || local.IsExposed)
                    continue;
                var tokenDeclaration = new TokenDeclaration(m_Store, local, m_GraphView);
                if (local is LoopVariableDeclarationModel loopVariableDeclarationModel)
                    VseUtility.AddTokenIcon(tokenDeclaration, loopVariableDeclarationModel.TitleComponentIcon);
                VariableContainer.Add(tokenDeclaration);
            }

            foreach (var local in m_FunctionModel.FunctionVariableModels)
            {
                var tokenDeclaration = new TokenDeclaration(m_Store, local, m_GraphView);
                if (local is LoopVariableDeclarationModel loopVariableDeclarationModel)
                    VseUtility.AddTokenIcon(tokenDeclaration, loopVariableDeclarationModel.TitleComponentIcon);
                VariableContainer.Add(tokenDeclaration);
            }

            IGraphElementModel elementModelToRename = m_Store.GetState().EditorDataModel.ElementModelToRename;
            if (elementModelToRename == null)
                return;

            var elementToRename = VariableContainer.Children().OfType<TokenDeclaration>().FirstOrDefault(x => ReferenceEquals(x.Declaration, elementModelToRename));
            if (elementToRename != null && m_GraphView is VseGraphView vseGraphView)
                vseGraphView.UIController.ElementToRename = elementToRename;
        }

        void OnNewVariableButtonClicked()
        {
            bool canAddParam = m_FunctionModel.AllowChangesToModel;
            var stencil = Store.GetState().CurrentGraphModel.Stencil;

            if (s_Options == null)
            {
                s_Options = new[] { new GUIContent("Function Parameter"), new GUIContent("Function Variable") };
                s_EventOptions = new[] {s_Options[1]};
            }

            EditorUtility.DisplayCustomMenu(
                m_NewVariableButton.worldBound,
                canAddParam ? s_Options : s_EventOptions,
                -1,
                (data, options, i) => CreateFunctionField(
                    typeof(float).GenerateTypeHandle(stencil),
                    canAddParam && i == 0 ? SupportedFields.Parameter : SupportedFields.Variable),
                null);
        }

        internal void CreateFunctionField(TypeHandle type, SupportedFields field)
        {
            HashSet<string> existingNames;
            Action<string> createField;

            var functionModel = (IFunctionModel)stackModel;
            if (field == SupportedFields.Variable)
            {
                existingNames = new HashSet<string>(functionModel.FunctionVariableModels.Select(v => v.Name));
                createField = n => m_Store.Dispatch(new CreateFunctionVariableDeclarationAction(functionModel, n, type));
            }
            else
            {
                existingNames = new HashSet<string>(functionModel.FunctionParameterModels.Select(p => p.Name));
                createField = n => m_Store.Dispatch(new CreateFunctionParameterDeclarationAction(functionModel, n, type));
            }

            var fieldName = field.ToString().GetUniqueName(existingNames);
            createField(fieldName);
        }

        protected override bool AcceptsElement(GraphElement element, ref int proposedIndex, int maxIndex)
        {
            if (element is TokenDeclaration tokenDeclaration)
                return ((VariableDeclarationModel)tokenDeclaration.Declaration).FunctionModel != stackModel;
            return base.AcceptsElement(element, ref proposedIndex, maxIndex);
        }

        public override bool DragPerform(DragPerformEvent evt, IEnumerable<ISelectable> selection, IDropTarget dropTarget, ISelection dragSource)
        {
            List<TokenDeclaration> tokenDeclarations = selection.OfType<TokenDeclaration>().ToList();
            List<IVariableDeclarationModel> tokenDeclarationModels = tokenDeclarations.Select(x => x.Declaration).ToList();
            if (tokenDeclarationModels.Any())
            {
                m_Store.Dispatch(
                    new DuplicateFunctionVariableDeclarationsAction((IFunctionModel)stackModel, tokenDeclarationModels));
                ((VseGraphView)m_GraphView).ClearPlaceholdersAfterDrag();
                return true;
            }

            return base.DragPerform(evt, selection, dropTarget, dragSource);
        }

        public override bool DragUpdated(DragUpdatedEvent evt, IEnumerable<ISelectable> selection, IDropTarget dropTarget, ISelection dragSource)
        {
            return selection.All(x => x is TokenDeclaration) || base.DragUpdated(evt, selection, dropTarget, dragSource);
        }

        public virtual void FilterOutChildrenFromSelection()
        {
            if (m_GraphView == null)
                return;

            foreach (var functionNodeVariable in Variables.Where(v => m_GraphView.selection.Contains(v)))
                m_GraphView.RemoveFromSelection(functionNodeVariable);
        }
    }
}
