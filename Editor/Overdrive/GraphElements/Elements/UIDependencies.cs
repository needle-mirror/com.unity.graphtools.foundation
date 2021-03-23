using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public class UIDependencies
    {
        IModelUI m_Owner;

        // Graph elements that we affect when we change.
        HashSet<(IModelUI, DependencyType)> m_ForwardDependencies;
        // Graph elements that affect us when they change.
        HashSet<(IModelUI, DependencyType)> m_BackwardDependencies;
        // Additional models that influence us.
        HashSet<IGraphElementModel> m_ModelDependencies;

        public UIDependencies(IModelUI owner)
        {
            m_Owner = owner;
        }

        public void ClearDependencyLists()
        {
            m_ForwardDependencies?.Clear();

            if (m_BackwardDependencies != null)
            {
                foreach (var (graphElement, dependencyType) in m_BackwardDependencies)
                {
                    if (dependencyType.HasFlagFast(DependencyType.Style))
                        (graphElement as VisualElement)?.UnregisterCallback<CustomStyleResolvedEvent>(OnBackwardDependencyCustomStyleResolved);
                    if (dependencyType.HasFlagFast(DependencyType.Geometry))
                        (graphElement as VisualElement)?.UnregisterCallback<GeometryChangedEvent>(OnBackwardDependencyGeometryChanged);
                    if (dependencyType.HasFlagFast(DependencyType.Removal))
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

        public void UpdateDependencyLists()
        {
            ClearDependencyLists();

            m_Owner.AddForwardDependencies();

            m_Owner.AddBackwardDependencies();
            if (m_BackwardDependencies != null)
            {
                foreach (var (graphElement, dependencyType) in m_BackwardDependencies)
                {
                    if (dependencyType.HasFlagFast(DependencyType.Style))
                        (graphElement as VisualElement)?.RegisterCallback<CustomStyleResolvedEvent>(OnBackwardDependencyCustomStyleResolved);
                    if (dependencyType.HasFlagFast(DependencyType.Geometry))
                        (graphElement as VisualElement)?.RegisterCallback<GeometryChangedEvent>(OnBackwardDependencyGeometryChanged);
                    if (dependencyType.HasFlagFast(DependencyType.Removal))
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

        public void AddForwardDependency(IModelUI dependency, DependencyType dependencyType)
        {
            if (m_ForwardDependencies == null)
                m_ForwardDependencies = new HashSet<(IModelUI, DependencyType)>();

            m_ForwardDependencies.Add((dependency, dependencyType));
        }

        public void AddBackwardDependency(IModelUI dependency, DependencyType dependencyType)
        {
            if (m_BackwardDependencies == null)
                m_BackwardDependencies = new HashSet<(IModelUI, DependencyType)>();

            m_BackwardDependencies.Add((dependency, dependencyType));
        }

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
                    if (dependencyType.HasFlagFast(DependencyType.Geometry))
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
                    if (dependencyType.HasFlagFast(DependencyType.Style))
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
                    if (dependencyType.HasFlagFast(DependencyType.Removal))
                        graphElement.UpdateFromModel();
                }
            }
        }
    }
}
