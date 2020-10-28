using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.Actions
{
    public class ActionSanityTests
    {
        static string[] IgnoredActionTypes =
        {
            // Not undoable
            nameof(LoadGraphAssetAction)
        };

        static IEnumerable<Type> AllActions()
        {
            return TypeCache.GetTypesDerivedFrom<BaseAction>()
                .Where(a => !a.IsAbstract && !IgnoredActionTypes.Contains(a.Name) && !a.Namespace.Contains(".Tests"))
                .OrderBy(t => t.Namespace).ThenBy(t => t.Name);
        }

        [TestCaseSource(nameof(AllActions))]
        public void ActionHaveAnUndoString(Type t)
        {
            foreach (var constructor in t.GetConstructors())
            {
                var action = constructor.Invoke(constructor.GetParameters().Select(
                    parameterInfo => parameterInfo.ParameterType.IsValueType ?
                    Activator.CreateInstance(parameterInfo.ParameterType) : null).ToArray()) as BaseAction;

                Assert.IsNotNull(action);
                Assert.IsNotNull(action.UndoString);
                Assert.AreNotEqual("", action.UndoString);
            }
        }
    }
}
