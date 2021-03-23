using System;
using NUnit.Framework;
using UnityEditor.EditorCommon.Redux;
using UnityEngine;
using UnityEngine.TestTools;

// ReSharper disable AccessToStaticMemberViaDerivedType

namespace UnityEditor.VisualScriptingTests.Redux
{
    [Category("Redux")]
    class StoreTests
    {
        const int k_MockStateDefault = 1;

        class MockStore : Store<MockState>
        {
            public MockStore(MockState initialState)
                : base(initialState)
            {
            }

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
            store.Register(observer.Observe);

            store.Register<PassThroughAction>(MockReducers.PassThrough);
            store.Register<ChangeFooAction>(MockReducers.ReplaceFoo);
            store.Register<ChangeBarAction>(MockReducers.ReplaceBar);

            Assert.That(store.GetState().Foo, Is.EqualTo(k_MockStateDefault));
            store.Unregister(observer.Observe);
        }

        [Test]
        public void RegisteringTwiceThrows()
        {
            var observer = new MockObserver();
            var store = new MockStore(new MockState(k_MockStateDefault));

            store.Register<ChangeFooAction>(MockReducers.ReplaceFoo);
            Assert.Throws(typeof(InvalidOperationException), () => store.Register<ChangeFooAction>(MockReducers.ReplaceFoo));
            store.Register(observer.Observe);
            Assert.Throws(typeof(InvalidOperationException), () => store.Register(observer.Observe));
        }

        [Test]
        public void UnregisteringTwiceDoesNotThrow()
        {
            var observer = new MockObserver();
            var store = new MockStore(new MockState(k_MockStateDefault));

            store.Register<ChangeFooAction>(MockReducers.ReplaceFoo);
            store.Register(observer.Observe);

            store.Unregister(observer.Observe);
            store.Unregister(observer.Observe);

            store.Unregister<ChangeFooAction>();
            store.Unregister<ChangeFooAction>();
        }

        [Test]
        public void ShouldDispatchAction()
        {
            var store = new MockStore(new MockState(k_MockStateDefault));

            store.Register<PassThroughAction>(MockReducers.PassThrough);
            store.Register<ChangeFooAction>(MockReducers.ReplaceFoo);
            store.Register<ChangeBarAction>(MockReducers.ReplaceBar);

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

            store.Register<ChangeFooAction>(MockReducers.ReplaceFoo);
            store.Register<ChangeBarAction>(MockReducers.ReplaceBar);

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

            store.Register<PassThroughAction>(MockReducers.PassThrough);
            store.Register<ChangeFooAction>(MockReducers.ReplaceFoo);
            store.Register<ChangeBarAction>(MockReducers.ReplaceBar);

            LogAssert.Expect(LogType.Error, $"No reducer for action type {typeof(UnregisteredAction)}");
            store.Dispatch(new UnregisteredAction());
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void RegisteredObserverShouldBeCalledForEachActionDispatched()
        {
            var observer = new MockObserver();
            var store = new MockStore(new MockState(k_MockStateDefault));
            store.Register(observer.Observe);

            store.Register<PassThroughAction>(MockReducers.PassThrough);
            store.Register<ChangeFooAction>(MockReducers.ReplaceFoo);
            store.Register<ChangeBarAction>(MockReducers.ReplaceBar);

            store.Dispatch(new ChangeFooAction(20));
            Assert.That(observer.ActionObserved, Is.EqualTo(1));

            store.Dispatch(new ChangeBarAction(10));
            Assert.That(observer.ActionObserved, Is.EqualTo(2));

            store.Dispatch(new PassThroughAction());
            Assert.That(observer.ActionObserved, Is.EqualTo(3));

            // Unregistered observer should not be notified anymore
            store.Unregister(observer.Observe);

            store.Dispatch(new PassThroughAction());
            Assert.That(observer.ActionObserved, Is.EqualTo(3));
        }

        [Test]
        public void MultipleObserverAreSupported()
        {
            var observer1 = new MockObserver();
            var observer2 = new MockObserver();
            var store = new MockStore(new MockState(k_MockStateDefault));
            store.Register(observer1.Observe);
            store.Register(observer2.Observe);

            store.Register<PassThroughAction>(MockReducers.PassThrough);
            store.Register<ChangeFooAction>(MockReducers.ReplaceFoo);
            store.Register<ChangeBarAction>(MockReducers.ReplaceBar);

            store.Dispatch(new ChangeFooAction(10));
            Assert.That(observer1.ActionObserved, Is.EqualTo(1));
            Assert.That(observer2.ActionObserved, Is.EqualTo(1));

            store.Dispatch(new PassThroughAction());
            Assert.That(observer1.ActionObserved, Is.EqualTo(2));
            Assert.That(observer2.ActionObserved, Is.EqualTo(2));

            store.Unregister(observer1.Observe);
            store.Unregister(observer2.Observe);
        }
    }
}
