using System;
using NUnit.Framework;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.CommandSystem
{
    public class PersistedEditorStateTests
    {
        [Test]
        public void SettingAStateComponentAndGettingItWorks()
        {
            var persistedState = new PersistedEditorState("Assets/Tests/test1.asset");
            var viewGuid1 = GUID.Generate();

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

            PersistedEditorState.RemoveViewState(viewGuid1);
        }
    }
}
