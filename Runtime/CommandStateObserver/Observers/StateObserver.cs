using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityEngine.GraphToolsFoundation.CommandStateObserver
{
    /// <summary>
    /// Base class for state observers.
    /// </summary>
    /// <typeparam name="TState">The type of the observed state.</typeparam>
    public abstract class StateObserver<TState> : IInternalStateObserver, IStateObserver where TState : IState
    {
        List<(string, StateComponentVersion)> m_ObservedComponentVersions;
        List<string> m_ModifiedStateComponents;

        /// <inheritdoc />
        public IEnumerable<string> ObservedStateComponents => m_ObservedComponentVersions.Select(t => t.Item1);

        /// <inheritdoc />
        public IEnumerable<string> ModifiedStateComponents => m_ModifiedStateComponents;

        /// <summary>
        /// Initializes a new instance of the <see cref="StateObserver{TState}" /> class.
        /// </summary>
        /// <param name="observedStateComponents">The names of the observed state components.</param>
        protected StateObserver(params string[] observedStateComponents)
            : this(observedStateComponents, Enumerable.Empty<string>()) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="StateObserver{TState}" /> class.
        /// </summary>
        /// <param name="observedStateComponents">The names of the observed state components.</param>
        /// <param name="modifiedStateComponents">The names of the modified state components.</param>
        protected StateObserver(IEnumerable<string> observedStateComponents, IEnumerable<string> modifiedStateComponents)
        {
            m_ObservedComponentVersions = new List<(string, StateComponentVersion)>(
                observedStateComponents.Distinct().Select<string, (string, StateComponentVersion)>(s => (s, default)));
            m_ModifiedStateComponents = modifiedStateComponents.Distinct().ToList();
        }

        /// <inheritdoc/>
        StateComponentVersion IInternalStateObserver.GetLastObservedComponentVersion(string componentName)
        {
            var index = m_ObservedComponentVersions.FindIndex(v => v.Item1 == componentName);
            return index >= 0 ? m_ObservedComponentVersions[index].Item2 : default;
        }

        /// <inheritdoc />
        void IInternalStateObserver.UpdateObservedVersion(string componentName, StateComponentVersion newVersion)
        {
            var index = m_ObservedComponentVersions.FindIndex(v => v.Item1 == componentName);
            if (index >= 0)
                m_ObservedComponentVersions[index] = (componentName, newVersion);
        }

        /// <inheritdoc />
        public void Observe(IState state)
        {
            if (state is TState tState)
                Observe(tState);
        }

        /// <inheritdoc cref="Observe(IState)"/>
        protected abstract void Observe(TState state);
    }
}
