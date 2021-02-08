using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

// ReSharper disable AccessToStaticMemberViaDerivedType

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.CommandSystem
{
    [Category("CommandSystem")]
    class CommandDispatcherTests
    {
        const int k_MockStateDefault = 1;

        CommandDispatcher m_CommandDispatcher;

        MockGraphToolState GetState() => m_CommandDispatcher.GraphToolState as MockGraphToolState;

        [SetUp]
        public void SetUp()
        {
            m_CommandDispatcher = new CommandDispatcher(new MockGraphToolState(k_MockStateDefault));
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
            Assert.That(GetState().Foo, Is.EqualTo(k_MockStateDefault));
            Assert.That(GetState().Bar, Is.EqualTo(k_MockStateDefault));
        }

        [Test]
        public void RegisteringDoesNotChangeState()
        {
            var observer = new MockObserver();
            m_CommandDispatcher.RegisterObserver(observer.Observe);

            m_CommandDispatcher.RegisterCommandHandler<MockGraphToolState, PassThroughCommand>(MockCommandHandlers.PassThrough);
            m_CommandDispatcher.RegisterCommandHandler<MockGraphToolState, ChangeFooCommand>(MockCommandHandlers.ReplaceFoo);
            m_CommandDispatcher.RegisterCommandHandler<MockGraphToolState, ChangeBarCommand>(MockCommandHandlers.ReplaceBar);

            Assert.That(GetState().Foo, Is.EqualTo(k_MockStateDefault));
            m_CommandDispatcher.UnregisterObserver(observer.Observe);
        }

        [Test, Ignore("We allow overrides for now.")]
        public void RegisteringTwiceThrows()
        {
            var observer = new MockObserver();

            m_CommandDispatcher.RegisterCommandHandler<MockGraphToolState, ChangeFooCommand>(MockCommandHandlers.ReplaceFoo);
            Assert.Throws(typeof(InvalidOperationException), () => m_CommandDispatcher.RegisterCommandHandler<MockGraphToolState, ChangeFooCommand>(MockCommandHandlers.ReplaceFoo));
            m_CommandDispatcher.RegisterObserver(observer.Observe);
            Assert.Throws(typeof(InvalidOperationException), () => m_CommandDispatcher.RegisterObserver(observer.Observe));
        }

        [Test]
        public void UnregisteringTwiceDoesNotThrow()
        {
            var observer = new MockObserver();

            m_CommandDispatcher.RegisterCommandHandler<MockGraphToolState, ChangeFooCommand>(MockCommandHandlers.ReplaceFoo);
            m_CommandDispatcher.RegisterObserver(observer.Observe);

            m_CommandDispatcher.UnregisterObserver(observer.Observe);
            m_CommandDispatcher.UnregisterObserver(observer.Observe);

            m_CommandDispatcher.UnregisterCommandHandler<ChangeFooCommand>();
            m_CommandDispatcher.UnregisterCommandHandler<ChangeFooCommand>();
        }

        [Test]
        public void ShouldDispatchCommand()
        {
            m_CommandDispatcher.RegisterCommandHandler<MockGraphToolState, PassThroughCommand>(MockCommandHandlers.PassThrough);
            m_CommandDispatcher.RegisterCommandHandler<MockGraphToolState, ChangeFooCommand>(MockCommandHandlers.ReplaceFoo);
            m_CommandDispatcher.RegisterCommandHandler<MockGraphToolState, ChangeBarCommand>(MockCommandHandlers.ReplaceBar);

            m_CommandDispatcher.Dispatch(new ChangeFooCommand(10));
            Assert.That(GetState().Foo, Is.EqualTo(10));
            Assert.That(GetState().Bar, Is.EqualTo(k_MockStateDefault));

            m_CommandDispatcher.Dispatch(new ChangeFooCommand(20));
            Assert.That(GetState().Foo, Is.EqualTo(20));
            Assert.That(GetState().Bar, Is.EqualTo(k_MockStateDefault));

            m_CommandDispatcher.Dispatch(new ChangeBarCommand(15));
            Assert.That(GetState().Foo, Is.EqualTo(20));
            Assert.That(GetState().Bar, Is.EqualTo(15));

            m_CommandDispatcher.Dispatch(new ChangeBarCommand(30));
            Assert.That(GetState().Foo, Is.EqualTo(20));
            Assert.That(GetState().Bar, Is.EqualTo(30));

            m_CommandDispatcher.Dispatch(new PassThroughCommand());
            Assert.That(GetState().Foo, Is.EqualTo(20));
            Assert.That(GetState().Bar, Is.EqualTo(30));
        }

        [Test]
        public void DispatchedCommandShouldTriggerStateChangedAfterUpdate()
        {
            m_CommandDispatcher.RegisterCommandHandler<MockGraphToolState, ChangeFooCommand>(MockCommandHandlers.ReplaceFoo);
            m_CommandDispatcher.RegisterCommandHandler<MockGraphToolState, ChangeBarCommand>(MockCommandHandlers.ReplaceBar);

            var versionCount = m_CommandDispatcher.GraphToolState.Version;

            m_CommandDispatcher.Dispatch(new ChangeFooCommand(10));
            Assert.That(m_CommandDispatcher.GraphToolState.Version, Is.EqualTo(versionCount + 1));

            m_CommandDispatcher.Dispatch(new ChangeBarCommand(20));
            Assert.That(m_CommandDispatcher.GraphToolState.Version, Is.EqualTo(versionCount + 2));
        }

        [Test]
        public void DispatchingUnregisteredCommandShouldLogAnError()
        {
            m_CommandDispatcher.RegisterCommandHandler<MockGraphToolState, PassThroughCommand>(MockCommandHandlers.PassThrough);
            m_CommandDispatcher.RegisterCommandHandler<MockGraphToolState, ChangeFooCommand>(MockCommandHandlers.ReplaceFoo);
            m_CommandDispatcher.RegisterCommandHandler<MockGraphToolState, ChangeBarCommand>(MockCommandHandlers.ReplaceBar);

            LogAssert.Expect(LogType.Error, $"No handler for command type {typeof(UnregisteredCommand)}");
            m_CommandDispatcher.Dispatch(new UnregisteredCommand());
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void RegisteredObserverShouldBeCalledForEachCommandDispatched()
        {
            var observer = new MockObserver();
            m_CommandDispatcher.RegisterObserver(observer.Observe);

            m_CommandDispatcher.RegisterCommandHandler<MockGraphToolState, PassThroughCommand>(MockCommandHandlers.PassThrough);
            m_CommandDispatcher.RegisterCommandHandler<MockGraphToolState, ChangeFooCommand>(MockCommandHandlers.ReplaceFoo);
            m_CommandDispatcher.RegisterCommandHandler<MockGraphToolState, ChangeBarCommand>(MockCommandHandlers.ReplaceBar);

            m_CommandDispatcher.Dispatch(new ChangeFooCommand(20));
            Assert.That(observer.CommandObserved, Is.EqualTo(1));

            m_CommandDispatcher.Dispatch(new ChangeBarCommand(10));
            Assert.That(observer.CommandObserved, Is.EqualTo(2));

            m_CommandDispatcher.Dispatch(new PassThroughCommand());
            Assert.That(observer.CommandObserved, Is.EqualTo(3));

            // Unregistered observer should not be notified anymore
            m_CommandDispatcher.UnregisterObserver(observer.Observe);

            m_CommandDispatcher.Dispatch(new PassThroughCommand());
            Assert.That(observer.CommandObserved, Is.EqualTo(3));
        }

        [Test]
        public void MultipleObserverAreSupported()
        {
            var observer1 = new MockObserver();
            var observer2 = new MockObserver();
            m_CommandDispatcher.RegisterObserver(observer1.Observe);
            m_CommandDispatcher.RegisterObserver(observer2.Observe);

            m_CommandDispatcher.RegisterCommandHandler<MockGraphToolState, PassThroughCommand>(MockCommandHandlers.PassThrough);
            m_CommandDispatcher.RegisterCommandHandler<MockGraphToolState, ChangeFooCommand>(MockCommandHandlers.ReplaceFoo);
            m_CommandDispatcher.RegisterCommandHandler<MockGraphToolState, ChangeBarCommand>(MockCommandHandlers.ReplaceBar);

            m_CommandDispatcher.Dispatch(new ChangeFooCommand(10));
            Assert.That(observer1.CommandObserved, Is.EqualTo(1));
            Assert.That(observer2.CommandObserved, Is.EqualTo(1));

            m_CommandDispatcher.Dispatch(new PassThroughCommand());
            Assert.That(observer1.CommandObserved, Is.EqualTo(2));
            Assert.That(observer2.CommandObserved, Is.EqualTo(2));

            m_CommandDispatcher.UnregisterObserver(observer1.Observe);
            m_CommandDispatcher.UnregisterObserver(observer2.Observe);
        }
    }
}
