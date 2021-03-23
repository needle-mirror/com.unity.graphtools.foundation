using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = System.Object;

// ReSharper disable AccessToStaticMemberViaDerivedType

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.CommandSystem
{
    [Category("CommandSystem")]
    class CommandDispatcherTests
    {
        const int k_StateDefaultValue = 1;

        CommandDispatcher m_CommandDispatcher;

        TestGraphToolState GetState() => m_CommandDispatcher.GraphToolState as TestGraphToolState;

        [SetUp]
        public void SetUp()
        {
            m_CommandDispatcher = new CommandDispatcher(new TestGraphToolState(k_StateDefaultValue));
            CommandDispatcherHelper.RegisterDefaultCommandHandlers(m_CommandDispatcher);
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
            m_CommandDispatcher.RegisterCommandObserver(observer.Observe);
            Assert.That(GetState().FooBarStateComponent.Foo, Is.EqualTo(k_StateDefaultValue));
        }

        [Test]
        public void RegisteringCommandObserverTwiceThrows()
        {
            var observer = new TestCommandObserver();

            m_CommandDispatcher.RegisterCommandHandler<TestGraphToolState, ChangeFooCommand>(ChangeFooCommand.DefaultHandler);
            m_CommandDispatcher.RegisterCommandObserver(observer.Observe);

            Assert.Throws<InvalidOperationException>(() => m_CommandDispatcher.RegisterCommandObserver(observer.Observe));
        }

        [Test]
        public void UnregisteringCommandObserverTwiceDoesNotThrow()
        {
            var observer = new TestCommandObserver();

            m_CommandDispatcher.RegisterCommandObserver(observer.Observe);

            m_CommandDispatcher.UnregisterCommandObserver(observer.Observe);
            m_CommandDispatcher.UnregisterCommandObserver(observer.Observe);
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void RegisteringStateObserverDoesNotChangeState()
        {
            var observer = new TestStateObserver();
            m_CommandDispatcher.RegisterObserver(observer);
            Assert.That(GetState().FooBarStateComponent.Foo, Is.EqualTo(k_StateDefaultValue));
        }

        [Test]
        public void RegisteringStateObserverTwiceThrows()
        {
            var observer = new TestStateObserver();

            m_CommandDispatcher.RegisterObserver(observer);

            Assert.Throws<InvalidOperationException>(() => m_CommandDispatcher.RegisterObserver(observer));
        }

        [Test]
        public void UnregisteringStateObserverTwiceDoesNotThrow()
        {
            var observer = new TestStateObserver();

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

            var testState = m_CommandDispatcher.GraphToolState as TestGraphToolState;
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
            m_CommandDispatcher.RegisterCommandObserver(observer.Observe);

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
            m_CommandDispatcher.UnregisterCommandObserver(observer.Observe);

            m_CommandDispatcher.Dispatch(new PassThroughCommand());
            Assert.That(observer.CommandObserved, Is.EqualTo(3));
        }

        [Test]
        public void AllRegisteredCommandObserverShouldBeCalledForEachCommandDispatched()
        {
            var observer1 = new TestCommandObserver();
            var observer2 = new TestCommandObserver();
            m_CommandDispatcher.RegisterCommandObserver(observer1.Observe);
            m_CommandDispatcher.RegisterCommandObserver(observer2.Observe);

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
            m_CommandDispatcher.RegisterCommandObserver(observer1.Observe);

            m_CommandDispatcher.RegisterCommandHandler<TestGraphToolState, ChangeFooCommand>(ChangeFooCommand.DefaultHandler);
            Assert.That(observer1.CommandObserved, Is.EqualTo(0));

            m_CommandDispatcher.Dispatch(new ChangeFooCommand(10));
            Assert.That(observer1.CommandObserved, Is.EqualTo(1));

            m_CommandDispatcher.UnregisterCommandObserver(observer1.Observe);
            m_CommandDispatcher.Dispatch(new ChangeFooCommand(10));
            Assert.That(observer1.CommandObserved, Is.EqualTo(1));
        }

        [Test]
        public void StateObserverIsNotifiedWhenObservedStateIsModified()
        {
            var observer = new TestStateObserver();

            m_CommandDispatcher.RegisterObserver(observer);
            m_CommandDispatcher.RegisterCommandHandler<TestGraphToolState, ChangeFooCommand>(ChangeFooCommand.DefaultHandler);

            var testState = m_CommandDispatcher.GraphToolState as TestGraphToolState;
            Assert.IsNotNull(testState);
            Assert.IsTrue(observer.ObservedStateComponents.Contains(nameof(TestGraphToolState.FooBarStateComponent)));

            var initialObserverVersion = observer.GetLastObservedComponentVersion(nameof(TestGraphToolState.FooBarStateComponent));

            m_CommandDispatcher.Dispatch(new ChangeFooCommand(10));
            var version = testState.FooBarStateComponent.CurrentVersion;

            m_CommandDispatcher.NotifyObservers();

            // Observer version has changed
            Assert.AreNotEqual(initialObserverVersion, observer.GetLastObservedComponentVersion(nameof(TestGraphToolState.FooBarStateComponent)));
            // and is equal to observed state version.
            Assert.AreEqual(version, observer.GetLastObservedComponentVersion(nameof(TestGraphToolState.FooBarStateComponent)).Version);
        }

        [Test]
        public void StateObserverIsNotNotifiedAfterUnregistering()
        {
            var observer = new TestStateObserver();

            m_CommandDispatcher.RegisterObserver(observer);
            m_CommandDispatcher.RegisterCommandHandler<TestGraphToolState, ChangeFooCommand>(ChangeFooCommand.DefaultHandler);

            var testState = m_CommandDispatcher.GraphToolState as TestGraphToolState;
            Assert.IsNotNull(testState);
            Assert.IsTrue(observer.ObservedStateComponents.Contains(nameof(TestGraphToolState.FooBarStateComponent)));

            var initialObserverVersion = observer.GetLastObservedComponentVersion(nameof(TestGraphToolState.FooBarStateComponent));

            m_CommandDispatcher.Dispatch(new ChangeFooCommand(10));

            m_CommandDispatcher.UnregisterObserver(observer);
            m_CommandDispatcher.NotifyObservers();

            // Observer version did not change
            Assert.AreEqual(initialObserverVersion, observer.GetLastObservedComponentVersion(nameof(TestGraphToolState.FooBarStateComponent)));
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
            public void Observe(GraphToolState state)
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

            var sortedObservers = m_CommandDispatcher.SortObservers(observers);
            var indices = sortedObservers.Select(so => observers.IndexOf(so)).ToArray();
            Assert.AreEqual(expectedOrder, indices);
        }
    }
}
