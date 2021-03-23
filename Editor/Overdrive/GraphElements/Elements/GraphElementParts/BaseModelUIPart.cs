using System;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public abstract class BaseModelUIPart : IModelUIPart
    {
        public string PartName { get; }

        public ModelUIPartList PartList { get; } = new ModelUIPartList();

        public abstract VisualElement Root { get; }

        protected IGraphElementModel m_Model;

        protected IModelUI m_OwnerElement;

        protected string m_ParentClassName;

        protected BaseModelUIPart(string name, IGraphElementModel model, IModelUI ownerElement, string parentClassName)
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

        public void OwnerAddedToView()
        {
            PartOwnerAddedToView();

            foreach (var component in PartList)
            {
                component.OwnerAddedToView();
            }
        }

        public void OwnerRemovedFromView()
        {
            PartOwnerRemovedFromView();

            foreach (var component in PartList)
            {
                component.OwnerRemovedFromView();
            }
        }

        /// <summary>
        /// Creates the UI for this part.
        /// </summary>
        /// <param name="parent">The parent element to attach the created UI to.</param>
        protected abstract void BuildPartUI(VisualElement parent);

        /// <summary>
        /// Finalizes the building of the UI.
        /// </summary>
        /// <remarks>This is a good place to add stylesheets that need to have a higher priority than the stylesheets of the children.</remarks>
        protected virtual void PostBuildPartUI() { }

        /// <summary>
        /// Updates the part to reflect the assigned model.
        /// </summary>
        protected abstract void UpdatePartFromModel();

        protected virtual void PartOwnerAddedToView() { }
        protected virtual void PartOwnerRemovedFromView() { }
    }
}
