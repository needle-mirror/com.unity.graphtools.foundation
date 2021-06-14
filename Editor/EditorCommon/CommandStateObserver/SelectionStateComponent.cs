using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Holds the selected graph elements in the current view, for the current graph asset.
    /// </summary>
    [Serializable]
    public sealed class SelectionStateComponent : AssetViewStateComponent<SelectionStateComponent.StateUpdater>
    {
        /// <summary>
        /// Updater for <see cref="SelectionStateComponent"/>.
        /// </summary>
        public class StateUpdater : BaseUpdater<SelectionStateComponent>
        {
            /// <summary>
            /// Marks graph elements as selected or unselected.
            /// </summary>
            /// <param name="graphElementModels">The graph elements to select or unselect.</param>
            /// <param name="select">True if the graph elements should be selected.
            /// False is the graph elements should be unselected.</param>
            public void SelectElements(IReadOnlyCollection<IGraphElementModel> graphElementModels, bool select)
            {
                // If m_SelectedModels is not null, we maintain it. Otherwise, we let GetSelection rebuild it.

                if (select)
                {
                    m_State.m_SelectedModels = m_State.m_SelectedModels?.Concat(graphElementModels).Distinct().ToList();

                    var guidsToAdd = graphElementModels.Select(x => x.Guid.ToString());
                    m_State.m_Selection = m_State.m_Selection.Concat(guidsToAdd).Distinct().ToList();
                }
                else
                {
                    foreach (var graphElementModel in graphElementModels)
                    {
                        if (m_State.m_Selection.Remove(graphElementModel.Guid.ToString()))
                            m_State.m_SelectedModels?.Remove(graphElementModel);
                    }
                }

                m_State.CurrentChangeset.ChangedModels.AddRangeInternal(graphElementModels);
                m_State.SetUpdateType(UpdateType.Partial);
            }

            /// <summary>
            /// Marks graph element as selected or unselected.
            /// </summary>
            /// <param name="graphElementModel">The graph element to select or unselect.</param>
            /// <param name="select">True if the graph element should be selected.
            /// False is the graph element should be unselected.</param>
            public void SelectElement(IGraphElementModel graphElementModel, bool select)
            {
                if (select)
                {
                    if (m_State.m_SelectedModels != null && !m_State.m_SelectedModels.Contains(graphElementModel))
                        m_State.m_SelectedModels.Add(graphElementModel);

                    var guid = graphElementModel.Guid.ToString();
                    if (!m_State.m_Selection.Contains(guid))
                        m_State.m_Selection.Add(guid);
                }
                else
                {
                    if (m_State.m_Selection.Remove(graphElementModel.Guid.ToString()))
                        m_State.m_SelectedModels?.Remove(graphElementModel);
                }
            }

            /// <summary>
            /// Unselects all graph elements.
            /// </summary>
            public void ClearSelection(IGraphModel graphModel)
            {
                m_State.CurrentChangeset.ChangedModels.AddRangeInternal(m_State.GetSelection(graphModel));
                m_State.SetUpdateType(UpdateType.Partial);

                // If m_SelectedModels is not null, we maintain it. Otherwise, we let GetSelection rebuild it.
                m_State.m_SelectedModels?.Clear();
                m_State.m_Selection.Clear();
            }
        }

        /// <summary>
        /// Changeset class for <see cref="SelectionStateComponent"/>.
        /// </summary>
        public class Changeset : IChangeset
        {
            /// <summary>
            /// The changed models.
            /// </summary>
            public HashSet<IGraphElementModel> ChangedModels { get; }

            /// <summary>
            /// Initializes a new instance of the <see cref="Changeset" /> class.
            /// </summary>
            public Changeset()
            {
                ChangedModels = new HashSet<IGraphElementModel>();
            }

            /// <inheritdoc/>
            public void Clear()
            {
                ChangedModels.Clear();
            }

            /// <inheritdoc/>
            public void AggregateFrom(IEnumerable<IChangeset> changesets)
            {
                Clear();
                foreach (var cs in changesets)
                {
                    if (cs is Changeset changeset)
                    {
                        ChangedModels.AddRangeInternal(changeset.ChangedModels);
                    }
                }
            }
        }

        // Source of truth
        [SerializeField]
        List<string> m_Selection;

        // Cache of selected models, built using m_Selection, for use by GetSelection().
        List<IGraphElementModel> m_SelectedModels;

        ChangesetManager<Changeset> m_ChangesetManager = new ChangesetManager<Changeset>();
        Changeset CurrentChangeset => m_ChangesetManager.CurrentChangeset;

        /// <summary>
        /// Initializes a new instance of the <see cref="SelectionStateComponent" /> class.
        /// </summary>
        public SelectionStateComponent()
        {
            m_Selection = new List<string>();
            m_SelectedModels = new List<IGraphElementModel>();
        }

        /// <inheritdoc/>
        protected override void PushChangeset(uint version)
        {
            base.PushChangeset(version);

            // If update type is Complete, there is no need to push the changeset, as they cannot be used for an update.
            if (UpdateType != UpdateType.Complete)
                m_ChangesetManager.PushChangeset(version);
        }

        /// <inheritdoc/>
        public override void PurgeOldChangesets(uint untilVersion)
        {
            EarliestChangeSetVersion = m_ChangesetManager.PurgeOldChangesets(untilVersion, CurrentVersion);
            ResetUpdateType();
        }

        /// <inheritdoc />
        public override void SetUpdateType(UpdateType type, bool force = false)
        {
            base.SetUpdateType(type, force);

            // If update type is Complete, there is no need to keep the changesets, as they cannot be used for an update.
            if (UpdateType == UpdateType.Complete)
            {
                m_ChangesetManager.PurgeOldChangesets(CurrentVersion, CurrentVersion);
            }
        }

        /// <inheritdoc  cref="ChangesetManager{TChangeset}"/>
        public Changeset GetAggregatedChangeset(uint sinceVersion)
        {
            return m_ChangesetManager.GetAggregatedChangeset(sinceVersion, CurrentVersion);
        }

        /// <summary>
        /// Gets the list of selected graph element models. If not done yet, this
        /// function resolves the list of models from a list of GUID, using the graph.
        /// </summary>
        /// <param name="graph">The graph containing the selected models.</param>
        /// <returns>A list of selected graph element models.</returns>
        public IReadOnlyList<IGraphElementModel> GetSelection(IGraphModel graph)
        {
            if (m_SelectedModels == null)
            {
                if (graph == null)
                {
                    return new List<IGraphElementModel>();
                }

                m_SelectedModels = new List<IGraphElementModel>();
                foreach (var guid in m_Selection)
                {
                    if (graph.TryGetModelFromGuid(new SerializableGUID(guid), out var model))
                    {
                        Debug.Assert(model != null);
                        m_SelectedModels.Add(model);
                    }
                }
            }

            return m_SelectedModels;
        }

        /// <inheritdoc/>
        public override void AfterDeserialize()
        {
            base.AfterDeserialize();
            m_SelectedModels = null;
        }

        /// <summary>
        /// Checks is the graph element model is selected.
        /// </summary>
        /// <param name="model">The model to check.</param>
        /// <returns>True is the model is selected. False otherwise.</returns>
        public bool IsSelected(IGraphElementModel model)
        {
            return model != null && m_Selection.Contains(model.Guid.ToString());
        }

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
        }
    }
}
