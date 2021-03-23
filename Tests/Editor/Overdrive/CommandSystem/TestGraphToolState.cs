using System;
using System.Collections.Generic;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.CommandSystem
{
    class FooBarStateComponent : ViewStateComponent<FooBarStateComponent.StateUpdater>
    {
        internal class StateUpdater : BaseUpdater<FooBarStateComponent>
        {
            public int Foo { get => m_State.Foo; set => m_State.Foo = value; }
            public int Bar { get => m_State.Bar; set => m_State.Bar = value; }
        }

        public int Foo { get; private set; }
        public int Bar { get; private set; }

        public FooBarStateComponent(int init)
        {
            Foo = init;
            Bar = init;
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
        }
    }

    class TestGraphToolState : GraphToolState
    {
        public FooBarStateComponent FooBarStateComponent { get; }

        public TestGraphToolState(int init) : base(default, null)
        {
            FooBarStateComponent = new FooBarStateComponent(init);
            FooBarStateComponent.StateSlotName = nameof(FooBarStateComponent);
        }

        ~TestGraphToolState() => Dispose(false);

        /// <inheritdoc />
        public override IEnumerable<IStateComponent> AllStateComponents
        {
            get
            {
                foreach (var s in base.AllStateComponents)
                {
                    yield return s;
                }

                yield return FooBarStateComponent;
            }
        }
    }
}
