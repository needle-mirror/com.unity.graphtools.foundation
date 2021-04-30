using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// The UI part for blocks containers.
    /// </summary>
    public class ContextBlocksPart : BaseModelUIPart
    {
        VisualElement m_Root;

        /// <inheritdoc/>
        public override VisualElement Root => m_Root;

        /// <summary>
        /// Creates a new ContextBlocksPart.
        /// </summary>
        /// <param name="name">The name of the part to create.</param>
        /// <param name="model">The model which the part represents.</param>
        /// <param name="modelUI">The owner of the part to create.</param>
        /// <param name="parentClassName">The class name of the parent UI.</param>
        /// <returns>A newly created ContextBlocksPart.</returns>
        public static ContextBlocksPart Create(string name, IGraphElementModel model, IModelUI modelUI, string parentClassName)
        {
            if (model is IContextNodeModel contextModel)
            {
                return new ContextBlocksPart(name, contextModel, modelUI, parentClassName);
            }

            return null;
        }

        /// <summary>
        /// Creates a new ContextBlocksPart.
        /// </summary>
        /// <param name="name">The name of the part to create.</param>
        /// <param name="nodeModel">The model which the part represents.</param>
        /// <param name="ownerElement">The owner of the part to create.</param>
        /// <param name="parentClassName">The class name of the parent UI.</param>
        /// <returns>A newly created ContextBlocksPart.</returns>
        protected ContextBlocksPart(string name, IContextNodeModel nodeModel, IModelUI ownerElement, string parentClassName)
            : base(name, nodeModel, ownerElement, parentClassName) { }

        /// <inheritdoc/>
        protected override void BuildPartUI(VisualElement container)
        {
            m_Root = new Label { name = PartName, text = "blocks go here" };
            container.Add(m_Root);
        }

        /// <inheritdoc/>
        protected override void UpdatePartFromModel()
        { }
    }
}
