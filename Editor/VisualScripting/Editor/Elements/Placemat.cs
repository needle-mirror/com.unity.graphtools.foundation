#if UNITY_2020_1_OR_NEWER
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.VisualScripting.Editor
{
    class Placemat : Experimental.GraphView.Placemat, IHasGraphElementModel, IResizable, IContextualMenuBuilder, IMovable, ICustomColor
    {
        readonly Store m_Store;
        readonly PlacematModel m_Model;

        public IGraphElementModel GraphElementModel => m_Model;

        public Placemat(PlacematModel model, Store store, GraphView graphView)
        {
            m_Model = model;
            m_Store = store;

            base.SetPosition(model.Position);
            base.Color = model.Color;
            base.title = model.Title;
            base.ZOrder = model.ZOrder;
            base.Collapsed = model.Collapsed;

            capabilities = VseUtility.ConvertCapabilities(model);
        }

        public override string title
        {
            get => base.title;
            set
            {
                base.title = value;
                if (m_Model.Title != value)
                {
                    m_Store.Dispatch(new ChangePlacematTitleAction(title, m_Model));
                }
            }
        }

        void ICustomColor.ResetColor()
        {
            (this as ICustomColor).SetColor(PlacematModel.k_DefaultColor);
        }

        void ICustomColor.SetColor(Color c)
        {
            base.Color = c;
        }

        public override Color Color
        {
            get => base.Color;
            set
            {
                base.Color = value;
                if (m_Model.Color != value)
                {
                    m_Store.Dispatch(new ChangeElementColorAction(Color, null, new[] { m_Model }));
                }
            }
        }

        public override bool Collapsed
        {
            get => base.Collapsed;
            set
            {
                base.Collapsed = value;

                if (m_Model.Collapsed != value)
                {
                    var collapsedModels = CollapsedElements.OfType<IHasGraphElementModel>()
                        .Select(graphElement => graphElement.GraphElementModel.GetId()).ToList();
                    m_Store.Dispatch(new ExpandOrCollapsePlacematAction(Collapsed, collapsedModels, m_Model));
                }
            }
        }

        public void ShowHiddenElements()
        {
            // Clear collapsed elements without updating model.
            SetCollapsedElements(null);
        }

        public void InitCollapsedElementsFromModel()
        {
            if (m_Model?.HiddenElementsGuid != null && m_GraphView != null)
            {
                var collapsedElements = new List<GraphElement>();
                foreach (var elementModelGuid in m_Model.HiddenElementsGuid)
                {
                    var graphElements = m_GraphView.graphElements.ToList();
                    var graphElement = graphElements
                        .Find(e => (e as IHasGraphElementModel)?.GraphElementModel?.GetId() == elementModelGuid);
                    if (graphElement != null)
                        collapsedElements.Add(graphElement);
                }

                SetCollapsedElements(collapsedElements);
            }
        }

        void IContextualMenuBuilder.BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            BuildContextualMenu(evt);
            var index = evt.menu.MenuItems().FindIndex(item => (item as DropdownMenuAction)?.name == "Change Color...");
            evt.menu.RemoveItemAt(index);
            // Also remove separator.
            evt.menu.RemoveItemAt(index);
        }

        public void OnStartResize()
        {
        }

        public void OnResized()
        {
            SetPosition(layout);
            m_Store.Dispatch(new ChangePlacematPositionAction(layout, m_Model));
        }

        public void UpdatePinning()
        {
        }

        public bool NeedStoreDispatch => false;
    }
}
#endif
