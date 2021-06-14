using System;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// A state component holding data to control the tracing process.
    /// </summary>
    [Serializable]
    public class TracingControlStateComponent : AssetViewStateComponent<TracingControlStateComponent.StateUpdater>
    {
        /// <summary>
        /// The updater for the <see cref="TracingControlStateComponent"/>.
        /// </summary>
        public class StateUpdater : BaseUpdater<TracingControlStateComponent>
        {
            /// <inheritdoc  cref="TracingControlStateComponent.CurrentTracingTarget"/>
            public int CurrentTracingTarget
            {
                set
                {
                    if (m_State.m_CurrentTracingTarget != value)
                    {
                        m_State.m_CurrentTracingTarget = value;
                        m_State.SetUpdateType(UpdateType.Complete);
                    }
                }
            }

            /// <inheritdoc  cref="TracingControlStateComponent.CurrentTracingFrame"/>
            public int CurrentTracingFrame
            {
                // Getter is here for convenience of using increment and decrement operator.
                get => m_State.m_CurrentTracingFrame;
                set
                {
                    if (m_State.m_CurrentTracingFrame != value)
                    {
                        m_State.m_CurrentTracingFrame = value;
                        m_State.SetUpdateType(UpdateType.Complete);
                    }
                }
            }

            /// <inheritdoc  cref="TracingControlStateComponent.CurrentTracingStep"/>
            public int CurrentTracingStep
            {
                // Getter is here for convenience of using increment and decrement operator.
                get => m_State.m_CurrentTracingStep;
                set
                {
                    if (m_State.m_CurrentTracingStep != value)
                    {
                        m_State.m_CurrentTracingStep = value;
                        m_State.SetUpdateType(UpdateType.Complete);
                    }
                }
            }
        }

        [SerializeField]
        int m_CurrentTracingTarget = -1;

        [SerializeField]
        int m_CurrentTracingFrame;

        [SerializeField]
        int m_CurrentTracingStep;

        /// <summary>
        /// The current tracing target index.
        /// </summary>
        public int CurrentTracingTarget => m_CurrentTracingTarget;

        /// <summary>
        /// The current frame index.
        /// </summary>
        public int CurrentTracingFrame => m_CurrentTracingFrame;

        /// <summary>
        /// The current step index.
        /// </summary>
        public int CurrentTracingStep => m_CurrentTracingStep;

        /// <summary>
        /// Initializes a new instance of the <see cref="TracingControlStateComponent" /> class.
        /// </summary>
        public TracingControlStateComponent()
        {
            m_CurrentTracingStep = -1;
        }

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
        }
    }
}
