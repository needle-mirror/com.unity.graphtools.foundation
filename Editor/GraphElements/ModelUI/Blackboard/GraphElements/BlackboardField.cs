using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// A GraphElement to display a <see cref="IVariableDeclarationModel"/>.
    /// </summary>
    public class BlackboardField : GraphElement, IHighlightable
    {
        public static new readonly string ussClassName = "ge-blackboard-field";
        public static readonly string capsuleUssClassName = ussClassName.WithUssElement("capsule");
        public static readonly string nameLabelUssClassName = ussClassName.WithUssElement("name-label");
        public static readonly string iconUssClassName = ussClassName.WithUssElement("icon");
        public static readonly string typeLabelUssClassName = ussClassName.WithUssElement("type-label");
        public static readonly string highlightedModifierUssClassName = ussClassName.WithUssModifier("highlighted");
        public static readonly string readOnlyModifierUssClassName = ussClassName.WithUssModifier("read-only");
        public static readonly string writeOnlyModifierUssClassName = ussClassName.WithUssModifier("write-only");
        public static readonly string iconExposedModifierUssClassName = iconUssClassName.WithUssModifier("exposed");

        public static readonly string selectionBorderElementName = "selection-border";

        protected Label m_TypeLabel;
        protected VisualElement m_Icon;
        SelectionDropper m_SelectionDropper;

        public EditableLabel NameLabel { get; protected set; }

        public bool Highlighted
        {
            get => ClassListContains(highlightedModifierUssClassName);
            set => EnableInClassList(highlightedModifierUssClassName, value);
        }

        protected SelectionDropper SelectionDropper
        {
            get => m_SelectionDropper;
            set => this.ReplaceManipulator(ref m_SelectionDropper, value);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BlackboardField"/> class.
        /// </summary>
        public BlackboardField()
        {
            SelectionDropper = new SelectionDropper();
        }

        /// <inheritdoc />
        protected override void BuildElementUI()
        {
            base.BuildElementUI();

            var selectionBorder = new SelectionBorder { name = selectionBorderElementName };
            selectionBorder.AddToClassList(ussClassName.WithUssElement(selectionBorderElementName));
            Add(selectionBorder);

            var capsule = new VisualElement();
            capsule.AddToClassList(capsuleUssClassName);
            selectionBorder.ContentContainer.Add(capsule);

            m_Icon = new VisualElement();
            m_Icon.AddToClassList(iconUssClassName);
            capsule.Add(m_Icon);

            NameLabel = new EditableLabel { name = "name" };
            NameLabel.AddToClassList(nameLabelUssClassName);
            capsule.Add(NameLabel);

            m_TypeLabel = new Label() { name = "type-label" };
            m_TypeLabel.AddToClassList(typeLabelUssClassName);
            Add(m_TypeLabel);
        }

        /// <inheritdoc />
        protected override void PostBuildUI()
        {
            base.PostBuildUI();

            AddToClassList(ussClassName);
        }

        /// <inheritdoc />
        protected override void UpdateElementFromModel()
        {
            base.UpdateElementFromModel();

            if (Model is IVariableDeclarationModel variableDeclarationModel)
            {
                m_Icon.EnableInClassList(iconExposedModifierUssClassName, variableDeclarationModel.IsExposed);

                var typeName = variableDeclarationModel.DataType.GetMetadata(variableDeclarationModel.GraphModel.Stencil).FriendlyName;

                NameLabel.SetValueWithoutNotify(variableDeclarationModel.DisplayTitle);
                m_TypeLabel.text = typeName;

                EnableInClassList(readOnlyModifierUssClassName, (variableDeclarationModel.Modifiers & ModifierFlags.ReadOnly) != 0);
                EnableInClassList(writeOnlyModifierUssClassName, (variableDeclarationModel.Modifiers & ModifierFlags.WriteOnly) != 0);
            }
        }

        public bool ShouldHighlightItemUsage(IGraphElementModel elementModel)
        {
            var currentVariableModel = Model as IVariableDeclarationModel;
            switch (elementModel)
            {
                case IVariableNodeModel variableModel
                    when ReferenceEquals(variableModel.VariableDeclarationModel, currentVariableModel):
                case IVariableDeclarationModel variableDeclarationModel
                    when ReferenceEquals(variableDeclarationModel, currentVariableModel):
                    return true;
            }
            return false;
        }
    }
}
