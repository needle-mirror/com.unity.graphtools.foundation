using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

// ReSharper disable AccessToStaticMemberViaDerivedType

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.Redux
{
    [Category("Redux")]
    class StoreTests
    {
        const int k_MockStateDefault = 1;

        class MockStore : Store
        {
            public MockStore(MockState initialState)
                : base(initialState)
            {
            }

            public new MockState GetState() => base.GetState() as MockState;

            public override void Dispatch<TAction>(TAction action)
            {
                base.Dispatch(action);
                // For testing purpose, we force the StateChanged right now.
                // In most application, this would happen during the Update phase.
                InvokeStateChanged();
            }
        }

        [Test]
        public void GetStateShouldReturnInitialState()
        {
            var store = new MockStore(new MockState(k_MockStateDefault));
            Assert.That(store.GetState().Foo, Is.EqualTo(k_MockStateDefault));
            Assert.That(store.GetState().Bar, Is.EqualTo(k_MockStateDefault));
        }

        [Test]
        public void RegisteringDoesNotChangeState()
        {
            var observer = new MockObserver();
            var store = new MockStore(new MockState(k_MockStateDefault));
            store.RegisterObserver(observer.Observe);

            store.RegisterReducer<MockState, PassThroughAction>(MockReducers.PassThrough);
            store.RegisterReducer<MockState, ChangeFooAction>(MockReducers.ReplaceFoo);
            store.RegisterReducer<MockState, ChangeBarAction>(MockReducers.ReplaceBar);

            Assert.That(store.GetState().Foo, Is.EqualTo(k_MockStateDefault));
            store.UnregisterObserver(observer.Observe);
        }

        [Test, Ignore("We allow overrides for now.")]
        public void RegisteringTwiceThrows()
        {
            var observer = new MockObserver();
            var store = new MockStore(new MockState(k_MockStateDefault));

            store.RegisterReducer<MockState, ChangeFooAction>(MockReducers.ReplaceFoo);
            Assert.Throws(typeof(InvalidOperationException), () => store.RegisterReducer<MockState, ChangeFooAction>(MockReducers.ReplaceFoo));
            store.RegisterObserver(observer.Observe);
            Assert.Throws(typeof(InvalidOperationException), () => store.RegisterObserver(observer.Observe));
        }

        [Test]
        public void UnregisteringTwiceDoesNotThrow()
        {
            var observer = new MockObserver();
            var store = new MockStore(new MockState(k_MockStateDefault));

            store.RegisterReducer<MockState, ChangeFooAction>(MockReducers.ReplaceFoo);
            store.RegisterObserver(observer.Observe);

            store.UnregisterObserver(observer.Observe);
            store.UnregisterObserver(observer.Observe);

            store.UnregisterReducer<ChangeFooAction>();
            store.UnregisterReducer<ChangeFooAction>();
        }

        [Test]
        public void ShouldDispatchAction()
        {
            var store = new MockStore(new MockState(k_MockStateDefault));

            store.RegisterReducer<MockState, PassThroughAction>(MockReducers.PassThrough);
            store.RegisterReducer<MockState, ChangeFooAction>(MockReducers.ReplaceFoo);
            store.RegisterReducer<MockState, ChangeBarAction>(MockReducers.ReplaceBar);

            store.Dispatch(new ChangeFooAction(10));
            Assert.That(store.GetState().Foo, Is.EqualTo(10));
            Assert.That(store.GetState().Bar, Is.EqualTo(k_MockStateDefault));

            store.Dispatch(new ChangeFooAction(20));
            Assert.That(store.GetState().Foo, Is.EqualTo(20));
            Assert.That(store.GetState().Bar, Is.EqualTo(k_MockStateDefault));

            store.Dispatch(new ChangeBarAction(15));
            Assert.That(store.GetState().Foo, Is.EqualTo(20));
            Assert.That(store.GetState().Bar, Is.EqualTo(15));

            store.Dispatch(new ChangeBarAction(30));
            Assert.That(store.GetState().Foo, Is.EqualTo(20));
            Assert.That(store.GetState().Bar, Is.EqualTo(30));

            store.Dispatch(new PassThroughAction());
            Assert.That(store.GetState().Foo, Is.EqualTo(20));
            Assert.That(store.GetState().Bar, Is.EqualTo(30));
        }

        [Test]
        public void DispatchedActionShouldTriggerStateChangedAfterUpdate()
        {
            int stateChangedCount = 0;
            var store = new MockStore(new MockState(k_MockStateDefault));

            store.RegisterReducer<MockState, ChangeFooAction>(MockReducers.ReplaceFoo);
            store.RegisterReducer<MockState, ChangeBarAction>(MockReducers.ReplaceBar);

            store.StateChanged += () => { stateChangedCount++; };

            store.Dispatch(new ChangeFooAction(10));
            Assert.That(stateChangedCount, Is.EqualTo(1));

            store.Dispatch(new ChangeBarAction(20));
            Assert.That(stateChangedCount, Is.EqualTo(2));
        }

        [Test]
        public void DispatchingUnregisteredActionShouldLogAnError()
        {
            var store = new MockStore(new MockState(k_MockStateDefault));

            store.RegisterReducer<MockState, PassThroughAction>(MockReducers.PassThrough);
            store.RegisterReducer<MockState, ChangeFooAction>(MockReducers.ReplaceFoo);
            store.RegisterReducer<MockState, ChangeBarAction>(MockReducers.ReplaceBar);

            LogAssert.Expect(LogType.Error, $"No reducer for action type {typeof(UnregisteredAction)}");
            store.Dispatch(new UnregisteredAction());
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void RegisteredObserverShouldBeCalledForEachActionDispatched()
        {
            var observer = new MockObserver();
            var store = new MockStore(new MockState(k_MockStateDefault));
            store.RegisterObserver(observer.Observe);

            store.RegisterReducer<MockState, PassThroughAction>(MockReducers.PassThrough);
            store.RegisterReducer<MockState, ChangeFooAction>(MockReducers.ReplaceFoo);
            store.RegisterReducer<MockState, ChangeBarAction>(MockReducers.ReplaceBar);

            store.Dispatch(new ChangeFooAction(20));
            Assert.That(observer.ActionObserved, Is.EqualTo(1));

            store.Dispatch(new ChangeBarAction(10));
            Assert.That(observer.ActionObserved, Is.EqualTo(2));

            store.Dispatch(new PassThroughAction());
            Assert.That(observer.ActionObserved, Is.EqualTo(3));

            // Unregistered observer should not be notified anymore
            store.UnregisterObserver(observer.Observe);

            store.Dispatch(new PassThroughAction());
            Assert.That(observer.ActionObserved, Is.EqualTo(3));
        }

        [Test]
        public void MultipleObserverAreSupported()
        {
            var observer1 = new MockObserver();
            var observer2 = new MockObserver();
            var store = new MockStore(new MockState(k_MockStateDefault));
            store.RegisterObserver(observer1.Observe);
            store.RegisterObserver(observer2.Observe);

            store.RegisterReducer<MockState, PassThroughAction>(MockReducers.PassThrough);
            store.RegisterReducer<MockState, ChangeFooAction>(MockReducers.ReplaceFoo);
            store.RegisterReducer<MockState, ChangeBarAction>(MockReducers.ReplaceBar);

            store.Dispatch(new ChangeFooAction(10));
            Assert.That(observer1.ActionObserved, Is.EqualTo(1));
            Assert.That(observer2.ActionObserved, Is.EqualTo(1));

            store.Dispatch(new PassThroughAction());
            Assert.That(observer1.ActionObserved, Is.EqualTo(2));
            Assert.That(observer2.ActionObserved, Is.EqualTo(2));

            store.UnregisterObserver(observer1.Observe);
            store.UnregisterObserver(observer2.Observe);
        }
    }
}
