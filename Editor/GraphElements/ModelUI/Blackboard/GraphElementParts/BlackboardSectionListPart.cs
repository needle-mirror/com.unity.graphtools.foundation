using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// A part to build the UI for the blackboard section list.
    /// </summary>
    public class BlackboardSectionListPart : BaseModelUIPart
    {
        public static readonly string ussClassName = "ge-blackboard-section-list-part";

        /// <summary>
        /// Creates a new instance of the <see cref="BlackboardSectionListPart"/> class.
        /// </summary>
        /// <param name="name">The name of the part.</param>
        /// <param name="model">The model displayed in this part.</param>
        /// <param name="ownerElement">The owner of the part.</param>
        /// <param name="parentClassName">The class name of the parent.</param>
        /// <returns>A new instance of <see cref="BlackboardSectionListPart"/>.</returns>
        public static BlackboardSectionListPart Create(string name, IGraphElementModel model, IModelUI ownerElement, string parentClassName)
        {
            if (model is IBlackboardGraphModel)
            {
                return new BlackboardSectionListPart(name, model, ownerElement, parentClassName);
            }

            return null;
        }

        public Blackboard Blackboard => m_OwnerElement as Blackboard;

        protected VisualElement m_Root;
        protected Dictionary<string, BlackboardSection> m_Sections;
        protected List<GraphElement> m_Rows = new List<GraphElement>();

        /// <summary>
        /// Initializes a new instance of the <see cref="BlackboardSectionListPart"/> class.
        /// </summary>
        /// <param name="name">The name of the part.</param>
        /// <param name="model">The model displayed in this part.</param>
        /// <param name="ownerElement">The owner of the part.</param>
        /// <param name="parentClassName">The class name of the parent.</param>
        protected BlackboardSectionListPart(string name, IGraphElementModel model, IModelUI ownerElement, string parentClassName)
            : base(name, model, ownerElement, parentClassName) { }

        /// <inheritdoc />
        public override VisualElement Root => m_Root;

        /// <inheritdoc />
        protected override void BuildPartUI(VisualElement parent)
        {
            m_Root = new VisualElement { name = PartName };
            m_Root.AddToClassList(ussClassName);
            m_Root.AddToClassList(m_ParentClassName.WithUssElement(PartName));

            if (m_Model is IBlackboardGraphModel graphProxyElement)
            {
                m_Sections = new Dictionary<string, BlackboardSection>();
                foreach (var sectionName in graphProxyElement.SectionNames)
                {
                    var section = new BlackboardSection(Blackboard, sectionName);
                    m_Root.Add(section);
                    m_Sections.Add(sectionName, section);
                }
            }

            parent.Add(m_Root);
        }

        /// <inheritdoc />
        protected override void UpdatePartFromModel()
        {
            if (m_Model is IBlackboardGraphModel graphProxyElement)
            {
                foreach (var row in m_Rows)
                {
                    row.RemoveFromView();
                }
                m_Rows.Clear();

                Blackboard.Highlightables.Clear();

                foreach (var sectionName in graphProxyElement.SectionNames)
                {
                    // PF FIXME: implement partial rebuild, like for PortContainer
                    m_Sections[sectionName].Clear();

                    var variableDeclarationModels = graphProxyElement.GetSectionRows(sectionName);
                    foreach (var vdm in variableDeclarationModels)
                    {
                        var ui = GraphElementFactory.CreateUI<GraphElement>(m_OwnerElement.View, m_OwnerElement.CommandDispatcher, vdm);

                        if (ui == null)
                            continue;

                        m_Sections[sectionName].Add(ui);
                        ui.AddToView(m_OwnerElement.View);
                        m_Rows.Add(ui);

                        Blackboard.Highlightables.AddRange(vdm.GetAllUIs(m_OwnerElement.View).OfType<IHighlightable>());
                    }
                }
            }
        }

        /// <inheritdoc />
        protected override void PartOwnerAddedToView()
        {
            foreach (var row in m_Rows)
            {
                row.AddToView(m_OwnerElement.View);
            }

            base.PartOwnerAddedToView();
        }

        /// <inheritdoc />
        protected override void PartOwnerRemovedFromView()
        {
            foreach (var row in m_Rows)
            {
                row.RemoveFromView();
            }

            base.PartOwnerRemovedFromView();
        }
    }
}
