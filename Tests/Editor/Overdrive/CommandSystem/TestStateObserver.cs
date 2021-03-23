using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.CommandSystem
{
    class TestStateObserver : StateObserver
    {
        /// <inheritdoc />
        public TestStateObserver()
            : base(nameof(TestGraphToolState.FooBarStateComponent)) { }

        /// <inheritdoc />
        public override void Observe(GraphToolState state)
        {
            if (state is TestGraphToolState testGraphToolState)
                using (this.ObserveState(testGraphToolState.FooBarStateComponent))
                {
                }
        }
    }
}
