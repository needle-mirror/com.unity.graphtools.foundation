using System;
using System.Linq;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Observer that updates a <see cref="Blackboard"/>.
    /// </summary>
    public class BlackboardUpdateObserver : StateObserver<GraphToolState>
    {
        protected Blackboard m_Blackboard;

        /// <summary>
        /// Initializes a new instance of the <see cref="BlackboardUpdateObserver" /> class.
        /// </summary>
        /// <param name="blackboard">The <see cref="Blackboard"/> to update.</param>
        public BlackboardUpdateObserver(Blackboard blackboard) :
            base(nameof(GraphToolState.GraphViewState), nameof(GraphToolState.BlackboardViewState), nameof(GraphToolState.WindowState))
        {
            m_Blackboard = blackboard;
        }

        /// <inheritdoc/>
        protected override void Observe(GraphToolState state)
        {
            // PF TODO be smarter about what needs to be updated.

            if (m_Blackboard?.panel != null)
            {
                using (var winObservation = this.ObserveState(state.WindowState))
                using (var gvObservation = this.ObserveState(state.GraphViewState))
                using (var bbObservation = this.ObserveState(state.BlackboardViewState))
                {
                    if (winObservation.UpdateType != UpdateType.None || gvObservation.UpdateType == UpdateType.Complete)
                    {
                        m_Blackboard.SetupBuildAndUpdate(state.WindowState.BlackboardGraphModel,
                            m_Blackboard.CommandDispatcher, m_Blackboard.View, m_Blackboard.Context);
                    }
                    else if (gvObservation.UpdateType == UpdateType.Partial)
                    {
                        var gvChangeSet = state.GraphViewState.GetAggregatedChangeset(gvObservation.LastObservedVersion);

                        if (gvChangeSet != null)
                        {
                            if (gvChangeSet.NewModels.OfType<IVariableDeclarationModel>().Any() ||
                                gvChangeSet.ChangedModels.OfType<IVariableDeclarationModel>().Any() ||
                                gvChangeSet.DeletedModels.OfType<IVariableDeclarationModel>().Any())
                            {
                                m_Blackboard?.UpdateFromModel();
                            }
                        }
                    }
                    else if (bbObservation.UpdateType != UpdateType.None)
                    {
                        var rows = m_Blackboard.Query<BlackboardRow>().Build().ToList();
                        foreach (var row in rows)
                        {
                            row.UpdateFromModel();
                        }
                    }
                }
            }
        }
    }
}
