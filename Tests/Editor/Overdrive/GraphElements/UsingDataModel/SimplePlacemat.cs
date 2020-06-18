#if DISABLE_SIMPLE_MATH_TESTS
using System;
using System.Collections.Generic;
using UnityEditor;
using Unity.GraphElements;
using UnityEngine;

namespace Editor.UsingDataModel.NoPresenters
{
    class SimplePlacemat : Placemat, IResizable
    {
        MathPlacemat m_Model;

        public MathPlacemat Model
        {
            get => m_Model;
            set
            {
                if (m_Model == value)
                    return;

                m_Model = value;
                OnDataChanged();

                if (m_Model)
                    EditorUtility.SetDirty(m_Model);
            }
        }

        public override string title
        {
            get => base.title;
            set
            {
                if (base.title == value)
                    return;

                base.title = value;
                if (userData is MathPlacemat mathPlacemat)
                    mathPlacemat.title = value;

                if (m_Model)
                    EditorUtility.SetDirty(m_Model);
            }
        }

        public override Color Color
        {
            get { return base.Color; }

            set
            {
                if (base.Color == value)
                    return;

                base.Color = value;

                if (userData is MathPlacemat mathPlacemat)
                    mathPlacemat.Color = value;

                if (m_Model)
                    EditorUtility.SetDirty(m_Model);
            }
        }

        public override bool Collapsed
        {
            get { return base.Collapsed; }

            set
            {
                if (base.Collapsed == value)
                    return;

                base.Collapsed = value;

                MathPlacemat mathPlacemat = userData as MathPlacemat;
                if (mathPlacemat == null)
                    return;

                if (mathPlacemat.Collapsed == value)
                {
                    return;
                }

                mathPlacemat.Collapsed = value;

                if (mathPlacemat.Collapsed)
                {
                    var collapsedModels = new List<string>();
                    foreach (var graphElement in CollapsedElements)
                    {
                        MathNode mathNode = graphElement.userData as MathNode;
                        if (mathNode != null)
                            collapsedModels.Add(mathNode.nodeID.ToString());
                        else
                        {
                            MathPlacemat mathPlacematChild = graphElement.userData as MathPlacemat;
                            if (mathPlacematChild != null)
                                collapsedModels.Add(mathPlacematChild.identification);
                        }
                    }

                    mathPlacemat.HiddenElementsId = collapsedModels;
                }
                else
                {
                    mathPlacemat.HiddenElementsId = null;
                }

                if (m_Model)
                    EditorUtility.SetDirty(m_Model);
            }
        }

        public override int ZOrder
        {
            set
            {
                if (base.ZOrder == value)
                    return;

                base.ZOrder = value;

                if (userData is MathPlacemat mathPlacemat)
                    mathPlacemat.zOrder = value;

                if (m_Model)
                    EditorUtility.SetDirty(m_Model);
            }
        }

        public override void SetPosition(Rect newPos)
        {
            base.SetPosition(newPos);

            if (userData is MathPlacemat mathPlacemat)
            {
                mathPlacemat.uncollapsedSize = ExpandedPosition.size;
                mathPlacemat.position = ExpandedPosition;
            }

            if (m_Model)
                EditorUtility.SetDirty(m_Model);
        }

        void OnDataChanged()
        {
            title = Model.title;
            if (Model.Collapsed && !Collapsed)
                SetPosition(new Rect(Model.position.position, Model.uncollapsedSize));
            else
                SetPosition(Model.position);
            Color = Model.Color;

            ZOrder = Model.zOrder;

            InitCollapsedElementsFromModel();
        }

        public void OnStartResize()
        {
        }

        public void OnResized()
        {
            SetPosition(layout);
        }

        internal void InitCollapsedElementsFromModel()
        {
            Collapsed = Model.Collapsed;

            MathPlacemat mathPlacemat = userData as MathPlacemat;
            if (mathPlacemat == null)
                return;

            if (mathPlacemat.HiddenElementsId != null && GraphView != null)
            {
                var collapsedElements = new List<GraphElement>();
                foreach (var elementModelGuid in mathPlacemat.HiddenElementsId)
                {
                    var graphElements = GraphView.graphElements.ToList();
                    var graphElement = graphElements
                        .Find(e => (e.userData as MathNode)?.nodeID.ToString() == elementModelGuid);
                    if (graphElement != null)
                        collapsedElements.Add(graphElement);
                    else
                    {
                        graphElement = graphElements
                            .Find(e => (e.userData as MathPlacemat)?.identification == elementModelGuid);
                        if (graphElement != null)
                            collapsedElements.Add(graphElement);
                    }
                }

                SetCollapsedElements(collapsedElements);
            }
        }
    }
}
#endif
