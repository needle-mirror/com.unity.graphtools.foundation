using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Stores dependencies of a UI to other UI and additional models.
    /// </summary>
    /// <remarks>
    /// - A dependency exists between two UI when one UI needs to be updated when another UI changes.
    /// - A dependency exists between a UI and a model when one UI needs to be updated when the model changes.
    ///   There is already an intrinsic dependency between a UI and its Model. However we sometimes need to
    ///   make a UI dependent on additional models.
    /// </remarks>
    public class UIDependencies
    {
        IModelUI m_Owner;

        // Graph elements that we affect when we change.
        HashSet<(IModelUI, DependencyTypes)> m_ForwardDependencies;
        // Graph elements that affect us when they change.
        HashSet<(IModelUI, DependencyTypes)> m_BackwardDependencies;
        // Additional models that influence us.
        HashSet<IGraphElementModel> m_ModelDependencies;

        /// <summary>
        /// Initializes a new instance of the <see cref="UIDependencies"/> class.
        /// </summary>
        /// <param name="owner">The UI for which these dependencies are declared.</param>
        public UIDependencies(IModelUI owner)
        {
            m_Owner = owner;
        }

        /// <summary>
        /// Removes all dependencies.
        /// </summary>
        public void ClearDependencyLists()
        {
            m_ForwardDependencies?.Clear();

            if (m_BackwardDependencies != null)
            {
                foreach (var (graphElement, dependencyType) in m_BackwardDependencies)
                {
                    if (dependencyType.HasFlagFast(DependencyTypes.Style))
                        (graphElement as VisualElement)?.UnregisterCallback<CustomStyleResolvedEvent>(OnBackwardDependencyCustomStyleResolved);
                    if (dependencyType.HasFlagFast(DependencyTypes.Geometry))
                        (graphElement as VisualElement)?.UnregisterCallback<GeometryChangedEvent>(OnBackwardDependencyGeometryChanged);
                    if (dependencyType.HasFlagFast(DependencyTypes.Removal))
                        (graphElement as VisualElement)?.UnregisterCallback<DetachFromPanelEvent>(OnBackwardDependencyDetachedFromPanel);
                }
            }

            m_BackwardDependencies?.Clear();

            if (m_ModelDependencies != null)
            {
                foreach (var model in m_ModelDependencies)
                {
                    model.RemoveDependency(m_Owner);
                }
            }

            m_ModelDependencies?.Clear();
        }

        /// <summary>
        /// Asks the owner UI to update its dependencies.
        /// </summary>
        public void UpdateDependencyLists()
        {
            ClearDependencyLists();

            m_Owner.AddForwardDependencies();

            m_Owner.AddBackwardDependencies();
            if (m_BackwardDependencies != null)
            {
                foreach (var (graphElement, dependencyType) in m_BackwardDependencies)
                {
                    if (dependencyType.HasFlagFast(DependencyTypes.Style))
                        (graphElement as VisualElement)?.RegisterCallback<CustomStyleResolvedEvent>(OnBackwardDependencyCustomStyleResolved);
                    if (dependencyType.HasFlagFast(DependencyTypes.Geometry))
                        (graphElement as VisualElement)?.RegisterCallback<GeometryChangedEvent>(OnBackwardDependencyGeometryChanged);
                    if (dependencyType.HasFlagFast(DependencyTypes.Removal))
                        (graphElement as VisualElement)?.RegisterCallback<DetachFromPanelEvent>(OnBackwardDependencyDetachedFromPanel);
                }
            }

            m_Owner.AddModelDependencies();
            if (m_ModelDependencies != null)
            {
                foreach (var model in m_ModelDependencies)
                {
                    model.AddDependency(m_Owner);
                }
            }
        }

        /// <summary>
        /// Adds a <see cref="ModelUI"/> to the forward dependencies list. A forward dependency is
        /// a UI that should be updated whenever this object's owner is updated.
        /// </summary>
        public void AddForwardDependency(IModelUI dependency, DependencyTypes dependencyType)
        {
            if (m_ForwardDependencies == null)
                m_ForwardDependencies = new HashSet<(IModelUI, DependencyTypes)>();

            m_ForwardDependencies.Add((dependency, dependencyType));
        }

        /// <summary>
        /// Adds a <see cref="ModelUI"/> to the backward dependencies list. A backward dependency is
        /// a UI that causes this object's owner to be updated whenever it is updated.
        /// </summary>
        public void AddBackwardDependency(IModelUI dependency, DependencyTypes dependencyType)
        {
            if (m_BackwardDependencies == null)
                m_BackwardDependencies = new HashSet<(IModelUI, DependencyTypes)>();

            m_BackwardDependencies.Add((dependency, dependencyType));
        }

        /// <summary>
        /// Adds <see cref="IGraphElementModel"/> to the model dependencies list. A model dependency is
        /// a graph element model that causes this object's owner to be updated whenever it is updated.
        /// </summary>
        public void AddModelDependency(IGraphElementModel model)
        {
            if (m_ModelDependencies == null)
                m_ModelDependencies = new HashSet<IGraphElementModel>();

            m_ModelDependencies.Add(model);
            model.AddDependency(m_Owner);
        }

        void OnBackwardDependencyGeometryChanged(GeometryChangedEvent evt)
        {
            m_Owner.UpdateFromModel();
        }

        void OnBackwardDependencyCustomStyleResolved(CustomStyleResolvedEvent evt)
        {
            m_Owner.UpdateFromModel();
        }

        void OnBackwardDependencyDetachedFromPanel(DetachFromPanelEvent evt)
        {
            m_Owner.UpdateFromModel();
        }

        public void OnGeometryChanged(GeometryChangedEvent evt)
        {
            if (m_ForwardDependencies != null)
            {
                foreach (var (graphElement, dependencyType) in m_ForwardDependencies)
                {
                    if (dependencyType.HasFlagFast(DependencyTypes.Geometry))
                        graphElement.UpdateFromModel();
                }
            }
        }

        public void OnCustomStyleResolved(CustomStyleResolvedEvent evt)
        {
            if (m_ForwardDependencies != null)
            {
                foreach (var (graphElement, dependencyType) in m_ForwardDependencies)
                {
                    if (dependencyType.HasFlagFast(DependencyTypes.Style))
                        graphElement.UpdateFromModel();
                }
            }
        }

        public void OnDetachedFromPanel(DetachFromPanelEvent evt)
        {
            if (m_ForwardDependencies != null)
            {
                foreach (var (graphElement, dependencyType) in m_ForwardDependencies)
                {
                    if (dependencyType.HasFlagFast(DependencyTypes.Removal))
                        graphElement.UpdateFromModel();
                }
            }
        }
    }
}
