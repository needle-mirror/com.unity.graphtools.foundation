using System;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// A <see cref="IDisposable"/> wrapper for <see cref="IStateComponentUpdater"/>.
    /// </summary>
    /// <remarks>
    /// Having a disposable wrapper enables the using() pattern.
    /// </remarks>
    /// <typeparam name="T">The type of updater to wrap.</typeparam>
    public readonly struct DisposableStateComponentUpdater<T> : IDisposable where T : class, IStateComponentUpdater
    {
        /// <summary>
        /// The wrapped updater.
        /// </summary>
        public T U { get; }

        /// <summary>
        /// Initializes a new instance of the StateComponentUpdaterWrapper class.
        /// </summary>
        /// <param name="wrapped">The updater to wrap.</param>
        public DisposableStateComponentUpdater(T wrapped)
        {
            U = wrapped;
            U.BeginStateChange();
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            U.EndStateChange();
        }
    }
}
