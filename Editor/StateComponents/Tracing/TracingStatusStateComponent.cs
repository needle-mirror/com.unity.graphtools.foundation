using System;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// A state component holding data to control the tracing process.
    /// </summary>
    [Serializable]
    public class TracingStatusStateComponent : AssetViewStateComponent<TracingStatusStateComponent.StateUpdater>
    {
        /// <summary>
        /// The updater for the <see cref="TracingStatusStateComponent"/>.
        /// </summary>
        public class StateUpdater : BaseUpdater<TracingStatusStateComponent>
        {
            /// <inheritdoc  cref="TracingStatusStateComponent.TracingEnabled"/>
            public bool TracingEnabled
            {
                set
                {
                    if (m_State.m_TracingEnabled != value)
                    {
                        m_State.m_TracingEnabled = value;
                        m_State.SetUpdateType(UpdateType.Complete);
                    }
                }
            }
        }

        [SerializeField]
        bool m_TracingEnabled;

        /// <summary>
        /// Whether tracing is enabled or not.
        /// </summary>
        public bool TracingEnabled => m_TracingEnabled;

        /// <summary>
        /// Initializes a new instance of the <see cref="TracingControlStateComponent" /> class.
        /// </summary>
        public TracingStatusStateComponent()
        {
        }

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
        }
    }
}
