using System.Linq;
using UnityEngine.UIElements;
// ReSharper disable InconsistentNaming

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// The UI part for vertical port containers.
    /// </summary>
    public class VerticalPortContainerPart : BaseModelUIPart
    {
        /// <summary>
        /// The USS class name for the part.
        /// </summary>
        public static readonly string ussClassName = "ge-vertical-port-container-part";

        /// <summary>
        /// The USS class name for the port container.
        /// </summary>
        public static readonly string portsUssName = "vertical-port-container";

        /// <summary>
        /// Creates a new VerticalPortContainerPart.
        /// </summary>
        /// <param name="name">The name of the part to create.</param>
        /// <param name="portDirection">The direction of the ports the container will hold.</param>
        /// <param name="model">The model which the part represents.</param>
        /// <param name="ownerUI">The owner of the part to create.</param>
        /// <param name="parentClassName">The class name of the parent UI.</param>
        /// <returns>A newly created VerticalPortContainerPart.</returns>
        public static VerticalPortContainerPart Create(string name, Direction portDirection, IGraphElementModel model, IModelUI ownerUI, string parentClassName)
        {
            if (model is IPortNodeModel)
            {
                return new VerticalPortContainerPart(name, portDirection, model, ownerUI, parentClassName);
            }

            return null;
        }

        /// <summary>
        /// The port container associated to this part.
        /// </summary>
        protected PortContainer m_PortContainer;

        VisualElement m_Root;

        Direction m_PortDirection;

        public override VisualElement Root => m_Root;

        /// <summary>
        /// Creates a new VerticalPortContainerPart.
        /// </summary>
        /// <param name="name">The name of the part to create.</param>
        /// <param name="portDirection">The direction of the ports the container will hold.</param>
        /// <param name="model">The model which the part represents.</param>
        /// <param name="ownerUI">The owner of the part to create.</param>
        /// <param name="parentClassName">The class name of the parent UI.</param>
        /// <returns>A newly created VerticalPortContainerPart.</returns>
        protected VerticalPortContainerPart(string name, Direction portDirection, IGraphElementModel model,
                                            IModelUI ownerUI, string parentClassName)
            : base(name, model, ownerUI, parentClassName)
        {
            m_PortDirection = portDirection;
        }

        /// <inheritdoc />
        protected override void BuildPartUI(VisualElement container)
        {
            if (m_Model is IInputOutputPortsNodeModel portNode)
            {
                m_Root = new VisualElement { name = PartName };
                m_Root.AddToClassList(ussClassName);
                m_Root.AddToClassList(m_ParentClassName.WithUssElement(PartName));

                m_PortContainer = new PortContainer { name = portsUssName };
                m_PortContainer.AddToClassList(m_ParentClassName.WithUssElement(portsUssName));
                m_Root.Add(m_PortContainer);

                var ports = (m_PortDirection == Direction.Input ? portNode.GetInputPorts() : portNode.GetOutputPorts())
                    .Where(p => p.Orientation == Orientation.Vertical);

                m_PortContainer?.UpdatePorts(ports, m_OwnerElement.GraphView, m_OwnerElement.CommandDispatcher);

                container.Add(m_Root);
            }
        }

        /// <inheritdoc />
        protected override void PostBuildPartUI()
        {
            base.PostBuildPartUI();
            m_Root.AddStylesheet("PortContainerPart.uss");
        }

        /// <inheritdoc />
        protected override void UpdatePartFromModel()
        {
            if (!(m_Model is IInputOutputPortsNodeModel portNode))
                return;

            var ports = (m_PortDirection == Direction.Input ? portNode.GetInputPorts() : portNode.GetOutputPorts())
                .Where(p => p.Orientation == Orientation.Vertical);

            m_PortContainer?.UpdatePorts(ports, m_OwnerElement.GraphView, m_OwnerElement.CommandDispatcher);
        }
    }
}
