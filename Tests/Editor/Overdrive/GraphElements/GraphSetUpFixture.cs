using System;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.Tests.GraphElements.Utilities;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GraphElements
{
    [SetUpFixture] // Need to forward this for NUnit to pick it up
    public class GraphSetUpFixture : GraphViewTestEnvironment {}
}
