using System;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;
using UnityEngine;

namespace UnityEngine.GraphToolsFoundation.Overdrive.Tests.CommandSystem
{
    class ObserverThatObservesFooBar : StateObserver<TestGraphToolState>
    {
        /// <inheritdoc />
        public ObserverThatObservesFooBar()
            : base(nameof(TestGraphToolState.FooBarStateComponent)) { }

        /// <inheritdoc />
        protected override void Observe(TestGraphToolState state)
        {
            using (this.ObserveState(state.FooBarStateComponent))
            {
            }
        }
    }

    class ObserverThatObservesFewBawAndModifiesFooBar : StateObserver<TestGraphToolState>
    {
        /// <inheritdoc />
        public ObserverThatObservesFewBawAndModifiesFooBar()
            : base(new[] { nameof(TestGraphToolState.FewBawStateComponent) },
                new[] { nameof(TestGraphToolState.FooBarStateComponent) })
        { }

        /// <inheritdoc />
        protected override void Observe(TestGraphToolState state)
        {
            using (this.ObserveState(state.FewBawStateComponent))
            {
                using (var updater = state.FooBarStateComponent.UpdateScope)
                    updater.Foo = 42;
            }
        }
    }
}
