using System.Linq;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// VisualElement to display a <see cref="IContextNodeModel"/>.
    /// </summary>
    public class ContextNode : CollapsibleInOutNode
    {
        protected IContextNodeModel ContextNodeModel => Model as IContextNodeModel;

        /// <summary>
        /// The USS class name used for context nodes
        /// </summary>
        public new static readonly string ussClassName = "ge-context-node";

        /// <summary>
        /// The name of the part containing the blocks
        /// </summary>
        public static readonly string blocksPartName = "blocks-container";

        /// <inheritdoc/>
        protected override void PostBuildUI()
        {
            var selectionBorder = this.SafeQ(selectionBorderElementName);
            var selectionBorderParent = selectionBorder.parent;

            //Move the selection border from being the entire container for the node to being on top of context-border
            while (selectionBorder.childCount > 0)
            {
                var elementAt = selectionBorder.ElementAt(0);
                selectionBorderParent.hierarchy.Add(elementAt); // use hierarchy because selectionBorderParent as a content container defined
            }

            var borderElement = new VisualElement() { name = "context-border" };
            contentContainer.Insert(0, borderElement);

            base.PostBuildUI();

            borderElement.Add(selectionBorder);

            this.AddStylesheet("ContextNode.uss");
            AddToClassList(ussClassName);
        }

        /// <inheritdoc/>
        protected override void UpdateElementFromModel()
        {
            base.UpdateElementFromModel();

            EnableInClassList("no-vertical-input", ContextNodeModel.InputsById.Values.All(t => t.Orientation != PortOrientation.Vertical));
            EnableInClassList("no-vertical-output", ContextNodeModel.OutputsById.Values.All(t => t.Orientation != PortOrientation.Vertical));
        }

        /// <inheritdoc/>
        protected override void BuildPartList()
        {
            base.BuildPartList();

            PartList.InsertPartBefore(bottomPortContainerPartName, ContextBlocksPart.Create(blocksPartName, NodeModel, this, ussClassName));
        }
    }
}

