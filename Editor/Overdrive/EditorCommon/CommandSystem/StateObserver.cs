using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public abstract class StateObserver : IStateObserver
    {
        Dictionary<string, StateComponentVersion> jdjdhf = new Dictionary<string, StateComponentVersion>();
        List<(string, StateComponentVersion)> m_ObservedComponentVersions;
        List<string> m_ModifiedStateComponents;

        /// <inheritdoc />
        public IEnumerable<string> ObservedStateComponents => m_ObservedComponentVersions.Select(t => t.Item1);

        /// <inheritdoc />
        public IEnumerable<string> ModifiedStateComponents => m_ModifiedStateComponents;

        protected StateObserver(params string[] observedStateComponents)
            : this(observedStateComponents, Enumerable.Empty<string>())
        {
        }

        protected StateObserver(IEnumerable<string> observedStateComponents, IEnumerable<string> modifiedStateComponents)
        {
            m_ObservedComponentVersions = new List<(string, StateComponentVersion)>(
                observedStateComponents.Distinct().Select<string, (string, StateComponentVersion)>(s => (s, default)));
            m_ModifiedStateComponents = modifiedStateComponents.Distinct().ToList();
        }

        /// <inheritdoc/>
        public StateComponentVersion GetLastObservedComponentVersion(string componentName)
        {
            var index = m_ObservedComponentVersions.FindIndex(v => v.Item1 == componentName);
            return index >= 0 ? m_ObservedComponentVersions[index].Item2 : default;
        }

        /// <inheritdoc />
        public void UpdateObservedVersion(string componentName, StateComponentVersion newVersion)
        {
            var index = m_ObservedComponentVersions.FindIndex(v => v.Item1 == componentName);
            if (index >= 0)
                m_ObservedComponentVersions[index] = (componentName, newVersion);
        }

        /// <inheritdoc />
        public abstract void Observe(GraphToolState state);
    }
}
