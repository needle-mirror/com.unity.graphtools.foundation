using System;
using System.Collections;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.GraphElements;
using UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting;
using UnityEngine;
using UnityEngine.TestTools;
using Node = UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.Node;

// ReSharper disable AccessToStaticMemberViaDerivedType

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.UI
{
    class CreationTests : BaseUIFixture
    {
        protected override bool CreateGraphOnStartup => true;
        protected override Type CreatedGraphType => typeof(ClassStencil);

        [UnityTest]
        public IEnumerator Test_CreateEmptyGraphClassStencil()
        {
            Assert.That(GetGraphElements().Count, Is.EqualTo(0));
            yield return null;
        }

        [UnityTest]
        public IEnumerator Test_MandatoryQ_Throw()
        {
            GraphModel.CreateNode<Type0FakeNodeModel>("Node0", Vector2.zero);

            Store.Dispatch(new RefreshUIAction(UpdateFlags.All));
            yield return null;

            Assert.That(GetGraphElements().Count, Is.EqualTo(1));
            var nodeUI = GetGraphElement(0);
            Assert.Throws<MissingUIElementException>(() => nodeUI.MandatoryQ("asdfghjkl"));
            Assert.DoesNotThrow(() => nodeUI.MandatoryQ(Node.k_TitleIconContainerPartName));
            yield return null;
        }
    }
}
