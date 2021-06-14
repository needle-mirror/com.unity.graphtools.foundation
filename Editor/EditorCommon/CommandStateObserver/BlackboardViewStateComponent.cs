using System;
using System.Collections.Generic;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// State component holding blackboard view related data.
    /// </summary>
    [Serializable]
    public class BlackboardViewStateComponent : AssetStateComponent<BlackboardViewStateComponent.StateUpdater>
    {
        /// <summary>
        /// The updater for the <see cref="BlackboardViewStateComponent"/>.
        /// </summary>
        public class StateUpdater : BaseUpdater<BlackboardViewStateComponent>
        {
            /// <summary>
            /// Sets the expanded state of the variable declaration model in the blackboard.
            /// </summary>
            /// <param name="model">The model for which to set the state.</param>
            /// <param name="expanded">True if the variable should be expanded, false otherwise.</param>
            public void SetVariableDeclarationModelExpanded(IVariableDeclarationModel model, bool expanded)
            {
                bool isExpanded = m_State.GetVariableDeclarationModelExpanded(model);
                if (isExpanded && !expanded)
                {
                    m_State.m_BlackboardExpandedRowStates?.Remove(model.Guid.ToString());
                    m_State.SetUpdateType(UpdateType.Complete);
                }
                else if (!isExpanded && expanded)
                {
                    m_State.m_BlackboardExpandedRowStates?.Add(model.Guid.ToString());
                    m_State.SetUpdateType(UpdateType.Complete);
                }
            }
        }

        [SerializeField]
        List<string> m_BlackboardExpandedRowStates;

        /// <summary>
        /// Initializes a new instance of the <see cref="BlackboardViewStateComponent" /> class.
        /// </summary>
        public BlackboardViewStateComponent()
        {
            m_BlackboardExpandedRowStates = new List<string>();
        }

        /// <summary>
        /// Gets the expanded state of a variable declaration model.
        /// </summary>
        /// <param name="model">The variable declaration model.</param>
        /// <returns>True is the UI for the model should be expanded. False otherwise.</returns>
        public bool GetVariableDeclarationModelExpanded(IVariableDeclarationModel model)
        {
            return m_BlackboardExpandedRowStates?.Contains(model.Guid.ToString()) ?? false;
        }

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
        }
    }
}
