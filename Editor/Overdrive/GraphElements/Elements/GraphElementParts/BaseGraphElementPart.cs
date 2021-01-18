using System;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public abstract class BaseGraphElementPart : IGraphElementPart
    {
        public string PartName { get; }

        public GraphElementPartList PartList { get; } = new GraphElementPartList();

        public abstract VisualElement Root { get; }

        protected IGraphElementModel m_Model;

        protected IGraphElement m_OwnerElement;

        protected string m_ParentClassName;

        protected BaseGraphElementPart(string name, IGraphElementModel model, IGraphElement ownerElement, string parentClassName)
        {
            PartName = name;
            m_Model = model;
            m_OwnerElement = ownerElement;
            m_ParentClassName = parentClassName;
        }

        public void BuildUI(VisualElement parent)
        {
            BuildPartUI(parent);

            if (Root != null)
            {
                foreach (var component in PartList)
                {
                    component.BuildUI(Root);
                }
            }
        }

        public void PostBuildUI()
        {
            foreach (var component in PartList)
            {
                component.PostBuildUI();
            }

            PostBuildPartUI();
        }

        public void UpdateFromModel()
        {
            UpdatePartFromModel();

            foreach (var component in PartList)
            {
                component.UpdateFromModel();
            }
        }

        public void OwnerAddedToGraphView()
        {
            PartOwnerAddedToGraphView();

            foreach (var component in PartList)
            {
                component.OwnerAddedToGraphView();
            }
        }

        public void OwnerRemovedFromGraphView()
        {
            PartOwnerRemovedFromGraphView();

            foreach (var component in PartList)
            {
                component.OwnerRemovedFromGraphView();
            }
        }

        protected abstract void BuildPartUI(VisualElement parent);

        protected virtual void PostBuildPartUI() {}

        protected abstract void UpdatePartFromModel();

        protected virtual void PartOwnerAddedToGraphView() {}
        protected virtual void PartOwnerRemovedFromGraphView() {}
    }
}
