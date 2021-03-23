using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor.VisualScripting.Editor;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model;
using UnityEditor.VisualScripting.Model.Stencils;
using UnityEngine;

// ReSharper disable AccessToStaticMemberViaDerivedType

namespace UnityEditor.VisualScriptingTests.Actions
{
    [Category("Macro")]
    class MacroFromMacroTests : BaseFixture
    {
        protected override bool CreateGraphOnStartup => true;
        protected override Type CreatedGraphType => typeof(MacroStencil);

        [SetUp]
        public new void SetUp()
        {
            var asset = AssetDatabase.LoadAssetAtPath<VSGraphAssetModel>(AssetDatabase.GUIDToAssetPath(k_GraphPath));
            ((MacroStencil)GraphModel.Stencil).SetParent(typeof(ClassStencil), GraphModel);
        }

        [Test]
        public void TestMacroParent()
        {
            m_Store.Dispatch(new RefactorExtractMacroAction(new List<IGraphElementModel>(), Vector2.zero, null));

            var macroRef = GraphModel.NodeModels.OfType<MacroRefNodeModel>().Single();
            Assert.That(macroRef, Is.Not.Null);
            Assert.That(macroRef.GraphAssetModel.GraphModel.Stencil, Is.TypeOf<MacroStencil>());
            Assert.That(((MacroStencil)macroRef.GraphAssetModel.GraphModel.Stencil).ParentType, Is.EqualTo(((MacroStencil)GraphModel.Stencil).ParentType));
            Assert.That(((MacroStencil)macroRef.GraphAssetModel.GraphModel.Stencil).ParentType, Is.EqualTo(typeof(ClassStencil)));
        }
    }
}
