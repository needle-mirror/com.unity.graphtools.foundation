using System;
using System.Collections;
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

        Store m_Store;

        MockState GetStoreState() => m_Store.State as MockState;

        [SetUp]
        public void SetUp()
        {
            m_Store = new Store(new MockState(k_MockStateDefault));
            StoreHelper.RegisterDefaultReducers(m_Store);
        }

        [TearDown]
        public void TearDown()
        {
            m_Store = null;
        }

        [Test]
        public void GetStateShouldReturnInitialState()
        {
            Assert.That(GetStoreState().Foo, Is.EqualTo(k_MockStateDefault));
            Assert.That(GetStoreState().Bar, Is.EqualTo(k_MockStateDefault));
        }

        [Test]
        public void RegisteringDoesNotChangeState()
        {
            var observer = new MockObserver();
            m_Store.RegisterObserver(observer.Observe);

            m_Store.RegisterReducer<MockState, PassThroughAction>(MockReducers.PassThrough);
            m_Store.RegisterReducer<MockState, ChangeFooAction>(MockReducers.ReplaceFoo);
            m_Store.RegisterReducer<MockState, ChangeBarAction>(MockReducers.ReplaceBar);

            Assert.That(GetStoreState().Foo, Is.EqualTo(k_MockStateDefault));
            m_Store.UnregisterObserver(observer.Observe);
        }

        [Test, Ignore("We allow overrides for now.")]
        public void RegisteringTwiceThrows()
        {
            var observer = new MockObserver();

            m_Store.RegisterReducer<MockState, ChangeFooAction>(MockReducers.ReplaceFoo);
            Assert.Throws(typeof(InvalidOperationException), () => m_Store.RegisterReducer<MockState, ChangeFooAction>(MockReducers.ReplaceFoo));
            m_Store.RegisterObserver(observer.Observe);
            Assert.Throws(typeof(InvalidOperationException), () => m_Store.RegisterObserver(observer.Observe));
        }

        [Test]
        public void UnregisteringTwiceDoesNotThrow()
        {
            var observer = new MockObserver();

            m_Store.RegisterReducer<MockState, ChangeFooAction>(MockReducers.ReplaceFoo);
            m_Store.RegisterObserver(observer.Observe);

            m_Store.UnregisterObserver(observer.Observe);
            m_Store.UnregisterObserver(observer.Observe);

            m_Store.UnregisterReducer<ChangeFooAction>();
            m_Store.UnregisterReducer<ChangeFooAction>();
        }

        [Test]
        public void ShouldDispatchAction()
        {
            m_Store.RegisterReducer<MockState, PassThroughAction>(MockReducers.PassThrough);
            m_Store.RegisterReducer<MockState, ChangeFooAction>(MockReducers.ReplaceFoo);
            m_Store.RegisterReducer<MockState, ChangeBarAction>(MockReducers.ReplaceBar);

            m_Store.Dispatch(new ChangeFooAction(10));
            Assert.That(GetStoreState().Foo, Is.EqualTo(10));
            Assert.That(GetStoreState().Bar, Is.EqualTo(k_MockStateDefault));

            m_Store.Dispatch(new ChangeFooAction(20));
            Assert.That(GetStoreState().Foo, Is.EqualTo(20));
            Assert.That(GetStoreState().Bar, Is.EqualTo(k_MockStateDefault));

            m_Store.Dispatch(new ChangeBarAction(15));
            Assert.That(GetStoreState().Foo, Is.EqualTo(20));
            Assert.That(GetStoreState().Bar, Is.EqualTo(15));

            m_Store.Dispatch(new ChangeBarAction(30));
            Assert.That(GetStoreState().Foo, Is.EqualTo(20));
            Assert.That(GetStoreState().Bar, Is.EqualTo(30));

            m_Store.Dispatch(new PassThroughAction());
            Assert.That(GetStoreState().Foo, Is.EqualTo(20));
            Assert.That(GetStoreState().Bar, Is.EqualTo(30));
        }

        [Test]
        public void DispatchedActionShouldTriggerStateChangedAfterUpdate()
        {
            m_Store.RegisterReducer<MockState, ChangeFooAction>(MockReducers.ReplaceFoo);
            m_Store.RegisterReducer<MockState, ChangeBarAction>(MockReducers.ReplaceBar);

            var versionCount = m_Store.State.Version;

            m_Store.Dispatch(new ChangeFooAction(10));
            Assert.That(m_Store.State.Version, Is.EqualTo(versionCount + 1));

            m_Store.Dispatch(new ChangeBarAction(20));
            Assert.That(m_Store.State.Version, Is.EqualTo(versionCount + 2));
        }

        [Test]
        public void DispatchingUnregisteredActionShouldLogAnError()
        {
            m_Store.RegisterReducer<MockState, PassThroughAction>(MockReducers.PassThrough);
            m_Store.RegisterReducer<MockState, ChangeFooAction>(MockReducers.ReplaceFoo);
            m_Store.RegisterReducer<MockState, ChangeBarAction>(MockReducers.ReplaceBar);

            LogAssert.Expect(LogType.Error, $"No reducer for action type {typeof(UnregisteredAction)}");
            m_Store.Dispatch(new UnregisteredAction());
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void RegisteredObserverShouldBeCalledForEachActionDispatched()
        {
            var observer = new MockObserver();
            m_Store.RegisterObserver(observer.Observe);

            m_Store.RegisterReducer<MockState, PassThroughAction>(MockReducers.PassThrough);
            m_Store.RegisterReducer<MockState, ChangeFooAction>(MockReducers.ReplaceFoo);
            m_Store.RegisterReducer<MockState, ChangeBarAction>(MockReducers.ReplaceBar);

            m_Store.Dispatch(new ChangeFooAction(20));
            Assert.That(observer.ActionObserved, Is.EqualTo(1));

            m_Store.Dispatch(new ChangeBarAction(10));
            Assert.That(observer.ActionObserved, Is.EqualTo(2));

            m_Store.Dispatch(new PassThroughAction());
            Assert.That(observer.ActionObserved, Is.EqualTo(3));

            // Unregistered observer should not be notified anymore
            m_Store.UnregisterObserver(observer.Observe);

            m_Store.Dispatch(new PassThroughAction());
            Assert.That(observer.ActionObserved, Is.EqualTo(3));
        }

        [Test]
        public void MultipleObserverAreSupported()
        {
            var observer1 = new MockObserver();
            var observer2 = new MockObserver();
            m_Store.RegisterObserver(observer1.Observe);
            m_Store.RegisterObserver(observer2.Observe);

            m_Store.RegisterReducer<MockState, PassThroughAction>(MockReducers.PassThrough);
            m_Store.RegisterReducer<MockState, ChangeFooAction>(MockReducers.ReplaceFoo);
            m_Store.RegisterReducer<MockState, ChangeBarAction>(MockReducers.ReplaceBar);

            m_Store.Dispatch(new ChangeFooAction(10));
            Assert.That(observer1.ActionObserved, Is.EqualTo(1));
            Assert.That(observer2.ActionObserved, Is.EqualTo(1));

            m_Store.Dispatch(new PassThroughAction());
            Assert.That(observer1.ActionObserved, Is.EqualTo(2));
            Assert.That(observer2.ActionObserved, Is.EqualTo(2));

            m_Store.UnregisterObserver(observer1.Observe);
            m_Store.UnregisterObserver(observer2.Observe);
        }
    }
}
