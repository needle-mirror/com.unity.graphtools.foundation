using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.EditorCommon.Extensions;
using UnityEditor.VisualScripting.Editor.SmartSearch;
using UnityEditor.VisualScripting.Model.Compilation;
using UnityEditor.VisualScripting.Model.Stencils;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.VisualScriptingTests.UI
{
    [SetUpFixture]
    // Since GraphView tests rely on some global state related to UIElements mouse capture
    // Here we make sure to disable input events on the whole editor UI to avoid other interactions
    // from interfering with the tests being run
    class GraphViewTestEnvironment
    {
        [OneTimeSetUp]
        public void RunBeforeAnyTests()
        {
            SetDisableInputEventsOnAllWindows(true);
            MouseCaptureController.ReleaseMouse();

            DataWatchServiceDisableThrottling(true);
        }

        [OneTimeTearDown]
        public void RunAfterAnyTests()
        {
            SetDisableInputEventsOnAllWindows(false);

            DataWatchServiceDisableThrottling(false);
        }

        static void SetDisableInputEventsOnAllWindows(bool value)
        {
            if (InternalEditorUtility.isHumanControllingUs == false)
                return;

            foreach (var otherWindow in Resources.FindObjectsOfTypeAll<EditorWindow>())
            {
                ChangeInputEvents(otherWindow, value);
            }
        }

        static void ChangeInputEvents(EditorWindow window, bool value)
        {
            try
            {
                typeof(EditorWindow).GetProperty("disableInputEvents", BindingFlags.NonPublic | BindingFlags.Static)?.SetValue(window, value);
            }
            catch
            {
                Debug.LogWarning("Unable to disableInputEvents");
            }
        }

        class TestStencil : Stencil
        {
            public override ISearcherDatabaseProvider GetSearcherDatabaseProvider()
            {
                return new ClassSearcherDatabaseProvider(this);
            }

            public override IBuilder Builder => null;
        }

        static void DataWatchServiceDisableThrottling(bool value)
        {
            try
            {
                var stencil = new TestStencil();
                var dataWatchServiceType = stencil.GetAssemblies()
                    .SelectMany(a => a.GetTypesSafe(), (domainAssembly, assemblyType) => assemblyType)
                    .First(x => x.Name == "DataWatchService");
                var sharedInstance = dataWatchServiceType.GetProperty("sharedInstance", BindingFlags.NonPublic | BindingFlags.Static)?.GetValue(null);
                dataWatchServiceType.GetProperty("disableThrottling", BindingFlags.NonPublic | BindingFlags.Static)?.SetValue(sharedInstance, value);
            }
            catch
            {
                Debug.LogWarning("DataWatchServiceDisableThrottling failed");
            }
        }
    }
}
