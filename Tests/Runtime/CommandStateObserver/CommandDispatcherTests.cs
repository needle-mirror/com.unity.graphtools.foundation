using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;
using UnityEngine;
using UnityEngine.TestTools;
using Dispatcher = UnityEngine.GraphToolsFoundation.CommandStateObserver.Dispatcher;

// ReSharper disable AccessToStaticMemberViaDerivedType

namespace UnityEngine.GraphToolsFoundation.Overdrive.Tests.CommandSystem
{
    [Category("CommandSystem")]
    class CommandDispatcherTests
    {
        const int k_StateDefaultValue = 1;

        Dispatcher m_CommandDispatcher;

        TestGraphToolState GetState() => m_CommandDispatcher.State as TestGraphToolState;

        [SetUp]
        public void SetUp()
        {
            m_CommandDispatcher = new Dispatcher(new TestGraphToolState(k_StateDefaultValue));
        }

        [TearDown]
        public void TearDown()
        {
            m_CommandDispatcher = null;
        }

        [Test]
        public void GetStateShouldReturnInitialState()
        {
            Assert.That(GetState().FooBarStateComponent.Foo, Is.EqualTo(k_StateDefaultValue));
            Assert.That(GetState().FooBarStateComponent.Bar, Is.EqualTo(k_StateDefaultValue));
        }

        [Test]
        public void RegisteringCommandObserverDoesNotChangeState()
        {
            var observer = new TestCommandObserver();
            m_CommandDispatcher.RegisterCommandPreDispatchCallback(observer.Observe);
            Assert.That(GetState().FooBarStateComponent.Foo, Is.EqualTo(k_StateDefaultValue));
        }

        [Test]
        public void RegisteringCommandObserverTwiceThrows()
        {
            var observer = new TestCommandObserver();

            m_CommandDispatcher.RegisterCommandHandler<TestGraphToolState, ChangeFooCommand>(ChangeFooCommand.DefaultHandler);
            m_CommandDispatcher.RegisterCommandPreDispatchCallback(observer.Observe);

            Assert.Throws<InvalidOperationException>(() => m_CommandDispatcher.RegisterCommandPreDispatchCallback(observer.Observe));
        }

        [Test]
        public void UnregisteringCommandObserverTwiceDoesNotThrow()
        {
            var observer = new TestCommandObserver();

            m_CommandDispatcher.RegisterCommandPreDispatchCallback(observer.Observe);

            m_CommandDispatcher.UnregisterCommandPreDispatchCallback(observer.Observe);
            m_CommandDispatcher.UnregisterCommandPreDispatchCallback(observer.Observe);
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void RegisteringStateObserverDoesNotChangeState()
        {
            var observer = new ObserverThatObservesFooBar();
            m_CommandDispatcher.RegisterObserver(observer);
            Assert.That(GetState().FooBarStateComponent.Foo, Is.EqualTo(k_StateDefaultValue));
        }

        [Test]
        public void RegisteringStateObserverTwiceThrows()
        {
            var observer = new ObserverThatObservesFooBar();

            m_CommandDispatcher.RegisterObserver(observer);

            Assert.Throws<InvalidOperationException>(() => m_CommandDispatcher.RegisterObserver(observer));
        }

        [Test]
        public void UnregisteringStateObserverTwiceDoesNotThrow()
        {
            var observer = new ObserverThatObservesFooBar();

            m_CommandDispatcher.RegisterObserver(observer);

            m_CommandDispatcher.UnregisterObserver(observer);
            m_CommandDispatcher.UnregisterObserver(observer);
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void DispatchCommandWorks()
        {
            m_CommandDispatcher.RegisterCommandHandler<TestGraphToolState, PassThroughCommand>(PassThroughCommand.PassThrough);
            m_CommandDispatcher.RegisterCommandHandler<TestGraphToolState, ChangeFooCommand>(ChangeFooCommand.DefaultHandler);
            m_CommandDispatcher.RegisterCommandHandler<TestGraphToolState, ChangeBarCommand>(ChangeBarCommand.DefaultHandler);

            m_CommandDispatcher.Dispatch(new ChangeFooCommand(10));
            Assert.That(GetState().FooBarStateComponent.Foo, Is.EqualTo(10));
            Assert.That(GetState().FooBarStateComponent.Bar, Is.EqualTo(k_StateDefaultValue));

            m_CommandDispatcher.Dispatch(new ChangeFooCommand(20));
            Assert.That(GetState().FooBarStateComponent.Foo, Is.EqualTo(20));
            Assert.That(GetState().FooBarStateComponent.Bar, Is.EqualTo(k_StateDefaultValue));

            m_CommandDispatcher.Dispatch(new ChangeBarCommand(15));
            Assert.That(GetState().FooBarStateComponent.Foo, Is.EqualTo(20));
            Assert.That(GetState().FooBarStateComponent.Bar, Is.EqualTo(15));

            m_CommandDispatcher.Dispatch(new ChangeBarCommand(30));
            Assert.That(GetState().FooBarStateComponent.Foo, Is.EqualTo(20));
            Assert.That(GetState().FooBarStateComponent.Bar, Is.EqualTo(30));

            m_CommandDispatcher.Dispatch(new PassThroughCommand());
            Assert.That(GetState().FooBarStateComponent.Foo, Is.EqualTo(20));
            Assert.That(GetState().FooBarStateComponent.Bar, Is.EqualTo(30));
        }

        [Test]
        public void DispatchedCommandShouldIncrementStateVersion()
        {
            m_CommandDispatcher.RegisterCommandHandler<TestGraphToolState, ChangeFooCommand>(ChangeFooCommand.DefaultHandler);
            m_CommandDispatcher.RegisterCommandHandler<TestGraphToolState, ChangeBarCommand>(ChangeBarCommand.DefaultHandler);

            var testState = m_CommandDispatcher.State as TestGraphToolState;
            Assert.IsNotNull(testState);
            var version = testState.FooBarStateComponent.CurrentVersion;

            m_CommandDispatcher.Dispatch(new ChangeFooCommand(10));
            Assert.That(testState.FooBarStateComponent.CurrentVersion, Is.GreaterThan(version));

            version = testState.FooBarStateComponent.CurrentVersion;
            m_CommandDispatcher.Dispatch(new ChangeBarCommand(20));
            Assert.That(testState.FooBarStateComponent.CurrentVersion, Is.GreaterThan(version));
        }

        [Test]
        public void DispatchingUnregisteredCommandShouldLogAnError()
        {
            m_CommandDispatcher.RegisterCommandHandler<TestGraphToolState, PassThroughCommand>(PassThroughCommand.PassThrough);
            m_CommandDispatcher.RegisterCommandHandler<TestGraphToolState, ChangeFooCommand>(ChangeFooCommand.DefaultHandler);
            m_CommandDispatcher.RegisterCommandHandler<TestGraphToolState, ChangeBarCommand>(ChangeBarCommand.DefaultHandler);

            LogAssert.Expect(LogType.Error, $"No handler for command type {typeof(UnregisteredCommand)}");
            m_CommandDispatcher.Dispatch(new UnregisteredCommand());
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void RegisteredCommandObserverShouldBeCalledForEachCommandDispatched()
        {
            var observer = new TestCommandObserver();
            m_CommandDispatcher.RegisterCommandPreDispatchCallback(observer.Observe);

            m_CommandDispatcher.RegisterCommandHandler<TestGraphToolState, PassThroughCommand>(PassThroughCommand.PassThrough);
            m_CommandDispatcher.RegisterCommandHandler<TestGraphToolState, ChangeFooCommand>(ChangeFooCommand.DefaultHandler);
            m_CommandDispatcher.RegisterCommandHandler<TestGraphToolState, ChangeBarCommand>(ChangeBarCommand.DefaultHandler);
            Assert.That(observer.CommandObserved, Is.EqualTo(0));

            m_CommandDispatcher.Dispatch(new ChangeFooCommand(20));
            Assert.That(observer.CommandObserved, Is.EqualTo(1));

            m_CommandDispatcher.Dispatch(new ChangeBarCommand(10));
            Assert.That(observer.CommandObserved, Is.EqualTo(2));

            m_CommandDispatcher.Dispatch(new PassThroughCommand());
            Assert.That(observer.CommandObserved, Is.EqualTo(3));

            // Unregistered observer should not be notified anymore
            m_CommandDispatcher.UnregisterCommandPreDispatchCallback(observer.Observe);

            m_CommandDispatcher.Dispatch(new PassThroughCommand());
            Assert.That(observer.CommandObserved, Is.EqualTo(3));
        }

        [Test]
        public void AllRegisteredCommandObserverShouldBeCalledForEachCommandDispatched()
        {
            var observer1 = new TestCommandObserver();
            var observer2 = new TestCommandObserver();
            m_CommandDispatcher.RegisterCommandPreDispatchCallback(observer1.Observe);
            m_CommandDispatcher.RegisterCommandPreDispatchCallback(observer2.Observe);

            m_CommandDispatcher.RegisterCommandHandler<TestGraphToolState, PassThroughCommand>(PassThroughCommand.PassThrough);
            m_CommandDispatcher.RegisterCommandHandler<TestGraphToolState, ChangeFooCommand>(ChangeFooCommand.DefaultHandler);
            m_CommandDispatcher.RegisterCommandHandler<TestGraphToolState, ChangeBarCommand>(ChangeBarCommand.DefaultHandler);
            Assert.That(observer1.CommandObserved, Is.EqualTo(0));
            Assert.That(observer2.CommandObserved, Is.EqualTo(0));

            m_CommandDispatcher.Dispatch(new ChangeFooCommand(10));
            Assert.That(observer1.CommandObserved, Is.EqualTo(1));
            Assert.That(observer2.CommandObserved, Is.EqualTo(1));

            m_CommandDispatcher.Dispatch(new PassThroughCommand());
            Assert.That(observer1.CommandObserved, Is.EqualTo(2));
            Assert.That(observer2.CommandObserved, Is.EqualTo(2));
        }

        [Test]
        public void CommandObserverShouldNotBeCalledAfterUnregistering()
        {
            var observer1 = new TestCommandObserver();
            m_CommandDispatcher.RegisterCommandPreDispatchCallback(observer1.Observe);

            m_CommandDispatcher.RegisterCommandHandler<TestGraphToolState, ChangeFooCommand>(ChangeFooCommand.DefaultHandler);
            Assert.That(observer1.CommandObserved, Is.EqualTo(0));

            m_CommandDispatcher.Dispatch(new ChangeFooCommand(10));
            Assert.That(observer1.CommandObserved, Is.EqualTo(1));

            m_CommandDispatcher.UnregisterCommandPreDispatchCallback(observer1.Observe);
            m_CommandDispatcher.Dispatch(new ChangeFooCommand(10));
            Assert.That(observer1.CommandObserved, Is.EqualTo(1));
        }

        [Test]
        public void StateObserverIsNotifiedWhenObservedStateIsModified()
        {
            var observer = new ObserverThatObservesFooBar();

            m_CommandDispatcher.RegisterObserver(observer);
            m_CommandDispatcher.RegisterCommandHandler<TestGraphToolState, ChangeFooCommand>(ChangeFooCommand.DefaultHandler);

            var testState = m_CommandDispatcher.State as TestGraphToolState;
            Assert.IsNotNull(testState);
            Assert.IsTrue(observer.ObservedStateComponents.Contains(nameof(TestGraphToolState.FooBarStateComponent)));

            var internalObserver = (IInternalStateObserver)observer;
            var initialObserverVersion = internalObserver.GetLastObservedComponentVersion(nameof(TestGraphToolState.FooBarStateComponent));

            m_CommandDispatcher.Dispatch(new ChangeFooCommand(10));
            var currentStateVersion = testState.FooBarStateComponent.CurrentVersion;

            m_CommandDispatcher.NotifyObservers();

            // Observer version has changed
            Assert.AreNotEqual(initialObserverVersion, internalObserver.GetLastObservedComponentVersion(nameof(TestGraphToolState.FooBarStateComponent)));
            // and is equal to current state version.
            Assert.AreEqual(currentStateVersion, internalObserver.GetLastObservedComponentVersion(nameof(TestGraphToolState.FooBarStateComponent)).Version);
        }

        [Test]
        public void StateObserverIsNotifiedWhenObservedStateIsModifiedByOtherObserver()
        {
            var observer1 = new ObserverThatObservesFooBar();
            var observer2 = new ObserverThatObservesFewBawAndModifiesFooBar();

            m_CommandDispatcher.RegisterObserver(observer1);
            m_CommandDispatcher.RegisterObserver(observer2);
            m_CommandDispatcher.RegisterCommandHandler<TestGraphToolState, ChangeFewCommand>(ChangeFewCommand.DefaultHandler);

            var testState = m_CommandDispatcher.State as TestGraphToolState;
            Assert.IsNotNull(testState);

            var internalObserver = (IInternalStateObserver)observer1;
            var initialObserverVersion = internalObserver.GetLastObservedComponentVersion(nameof(TestGraphToolState.FooBarStateComponent));

            m_CommandDispatcher.Dispatch(new ChangeFewCommand(10));
            var beforeNotification = testState.FooBarStateComponent.CurrentVersion;

            m_CommandDispatcher.NotifyObservers();
            var afterNotification = testState.FooBarStateComponent.CurrentVersion;

            // Observer version has changed since initial observation.
            Assert.AreNotEqual(initialObserverVersion, internalObserver.GetLastObservedComponentVersion(nameof(TestGraphToolState.FooBarStateComponent)));

            // Observer version has changed after notifying observers.
            Assert.AreNotEqual(beforeNotification, internalObserver.GetLastObservedComponentVersion(nameof(TestGraphToolState.FooBarStateComponent)));

            // and is equal to current state version.
            Assert.AreEqual(afterNotification, internalObserver.GetLastObservedComponentVersion(nameof(TestGraphToolState.FooBarStateComponent)).Version);
        }

        [Test]
        public void StateObserverIsNotNotifiedAfterUnregistering()
        {
            var observer = new ObserverThatObservesFooBar();

            m_CommandDispatcher.RegisterObserver(observer);
            m_CommandDispatcher.RegisterCommandHandler<TestGraphToolState, ChangeFooCommand>(ChangeFooCommand.DefaultHandler);

            var testState = m_CommandDispatcher.State as TestGraphToolState;
            Assert.IsNotNull(testState);
            Assert.IsTrue(observer.ObservedStateComponents.Contains(nameof(TestGraphToolState.FooBarStateComponent)));

            var internalObserver = (IInternalStateObserver)observer;
            var initialObserverVersion = internalObserver.GetLastObservedComponentVersion(nameof(TestGraphToolState.FooBarStateComponent));

            m_CommandDispatcher.Dispatch(new ChangeFooCommand(10));

            m_CommandDispatcher.UnregisterObserver(observer);
            m_CommandDispatcher.NotifyObservers();

            // Observer version did not change
            Assert.AreEqual(initialObserverVersion, internalObserver.GetLastObservedComponentVersion(nameof(TestGraphToolState.FooBarStateComponent)));
        }

        class OrderTestStateObserver : IStateObserver
        {
            readonly string[] m_ModifiedStateComponents;
            readonly string[] m_ObservedStateComponents;

            /// <inheritdoc />
            public IEnumerable<string> ObservedStateComponents => m_ObservedStateComponents;

            /// <inheritdoc />
            public IEnumerable<string> ModifiedStateComponents => m_ModifiedStateComponents;

            public OrderTestStateObserver(string[] observed, string[] updated)
            {
                m_ObservedStateComponents = observed;
                m_ModifiedStateComponents = updated;
            }

            /// <inheritdoc />
            public StateComponentVersion GetLastObservedComponentVersion(string componentName)
            {
                throw new NotImplementedException();
            }

            /// <inheritdoc />
            public void UpdateObservedVersion(string componentName, StateComponentVersion newVersion)
            {
                throw new NotImplementedException();
            }

            /// <inheritdoc />
            public void Observe(IState state)
            {
                throw new NotImplementedException();
            }
        }

        static List<IStateObserver> MakeObserverSet(IEnumerable<(string[] observed, string[] updated)> desc)
        {
            return desc.Select(d => new OrderTestStateObserver(d.observed, d.updated) as IStateObserver).ToList();
        }

        static IEnumerable<object[]> GetTestStateObservers()
        {
            yield return new object[]
            {
                MakeObserverSet(
                    new[]
                    {
                        (new[] { "a" }, new string[] { }),
                    }),
                new[] { 0 },
                false
            };
            yield return new object[]
            {
                MakeObserverSet(
                    new[]
                    {
                        (new[] { "a" }, new string[] { }),
                        (new[] { "b" }, new[] { "a" }),
                    }),
                new[] { 1, 0 },
                false
            };
            yield return new object[]
            {
                MakeObserverSet(
                    new[]
                    {
                        (new[] { "b" }, new[] { "a" }),
                        (new[] { "a" }, new string[] { }),
                    }),
                new[] { 0, 1 },
                false
            };
            yield return new object[]
            {
                MakeObserverSet(
                    new[]
                    {
                        (new[] { "a" }, new string[] { }),
                        (new[] { "b" }, new[] { "a" }),
                        (new[] { "d" }, new[] { "c" }),
                        (new[] { "c" }, new[] { "b" })
                    }),
                new[] { 2, 3, 1, 0 },
                false
            };
            yield return new object[]
            {
                MakeObserverSet(
                    new[]
                    {
                        (new[] { "a", "b" }, new[] { "c", "d", "e"}),
                        (new[] { "c", "d" }, new[] { "e" }),
                        (new[] { "a", "c" }, new[] { "d" }),
                        (new[] { "d" }, new[] { "e" })
                    }),
                new[] { 0, 2, 1, 3 },
                false
            };
            yield return new object[]
            {
                MakeObserverSet(
                    new[]
                    {
                        (new[] { "a" }, new[] { "b" }),
                        (new[] { "b" }, new[] { "a" }),
                    }),
                new[] { 0, 1 },
                true
            };
            yield return new object[]
            {
                MakeObserverSet(
                    new[]
                    {
                        (new[] { "a", "b" }, new[] { "c", "d", "e"}),
                        (new[] { "c", "d" }, new[] { "e" }),
                        (new[] { "a", "c" }, new[] { "d" }),
                        (new[] { "d" }, new[] { "e" }),
                        (new[] { "e" }, new[] { "a" }),
                    }),
                new[] { 0, 1, 2, 3, 4 },
                true
            };
            yield return new object[]
            {
                MakeObserverSet(
                    new[]
                    {
                        (new[] { "a", "b" }, new[] { "c", "d", "e"}),
                        (new[] { "c", "d" }, new[] { "e" }),
                        (new[] { "a", "c" }, new[] { "d" }),
                        (new[] { "d" }, new[] { "e" }),
                        (new[] { "e" }, new[] { "c" }),
                    }),
                new[] { 0, 1, 2, 3, 4 },
                true
            };
        }

        [Test, TestCaseSource(nameof(GetTestStateObservers))]
        public void StateObserversAreSortedAccordingToObservationsAndUpdates(List<IStateObserver> observers, int[] expectedOrder, bool expectedHasCycle)
        {
            if (expectedHasCycle)
                LogAssert.Expect(LogType.Warning, "Dependency cycle detected in observers.");

            Dispatcher.SortObservers(observers.ToList(), out var sortedObservers);
            var indices = sortedObservers.Select(so => observers.IndexOf(so)).ToArray();
            Assert.AreEqual(expectedOrder, indices);
        }
    }
}
