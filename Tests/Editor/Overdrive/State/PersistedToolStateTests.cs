using System;
using NUnit.Framework;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.CommandSystem
{
    public class PersistedToolStateTests
    {
        [Test]
        public void SettingAStateComponentAndGettingItWorks()
        {
            var persistedState = new PersistedState();
            persistedState.SetAssetKey("42424242");
            var viewGuid1 = SerializableGUID.Generate();

            // View State Components
            {
                var firstState = persistedState.GetOrCreateViewStateComponent<WindowStateComponent>(viewGuid1, "");
                var newStateComponent = new WindowStateComponent();
                persistedState.SetViewStateComponent(viewGuid1, newStateComponent);
                var secondState = persistedState.GetOrCreateViewStateComponent<WindowStateComponent>(viewGuid1, "");
                Assert.IsNotNull(firstState);
                Assert.IsNotNull(secondState);
                Assert.AreSame(newStateComponent, secondState);
            }

            // Asset State Components
            {
                var firstState = persistedState.GetOrCreateAssetStateComponent<BlackboardViewStateComponent>("");
                var newStateComponent = new BlackboardViewStateComponent();
                persistedState.SetAssetStateComponent(newStateComponent);
                var secondState = persistedState.GetOrCreateAssetStateComponent<BlackboardViewStateComponent>("");
                Assert.IsNotNull(firstState);
                Assert.IsNotNull(secondState);
                Assert.AreSame(newStateComponent, secondState);
            }

            // Asset View State Components
            {
                var firstState = persistedState.GetOrCreateAssetViewStateComponent<SelectionStateComponent>(viewGuid1, "");
                var newStateComponent = new SelectionStateComponent();
                persistedState.SetAssetViewStateComponent(viewGuid1, newStateComponent);
                var secondState = persistedState.GetOrCreateAssetViewStateComponent<SelectionStateComponent>(viewGuid1, "");
                Assert.IsNotNull(firstState);
                Assert.IsNotNull(secondState);
                Assert.AreSame(newStateComponent, secondState);
            }

            PersistedState.RemoveViewState(viewGuid1);
        }
    }
}
