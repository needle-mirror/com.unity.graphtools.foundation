using System;
using JetBrains.Annotations;
using NUnit.Framework;
using UnityEditor.VisualScripting.Model;
using UnityEditor.VisualScripting.Model.Stencils;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.VisualScripting;

// ReSharper disable AccessToStaticMemberViaDerivedType

namespace UnityEditor.VisualScriptingTests.Roslyn
{
    class InstrumentationTests : BaseFixture
    {
        protected override Type CreatedGraphType => typeof(ClassStencil);

        protected override bool CreateGraphOnStartup => true;

        class TestStencil : ClassStencil
        {
            [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
            internal abstract class TestArchetype
            {
                [ModelType(Type = typeof(EventFunctionModel))]
                [CreatedByDefault]
                public abstract void Start();
            }
        }

        [Test]
        public void InstrumentCooldownNodeModelDoesNotThrow()
        {
            var start = GraphModel.CreateEventFunction(typeof(TestStencil.TestArchetype).GetMethod("Start"), Vector2.zero);
            var cooldown = GraphModel.CreateLoopStack<ForEachHeaderModel>(Vector2.down);
            var loopNode = cooldown.CreateLoopNode(start, -1);
            GraphModel.CreateEdge(cooldown.InputPort, loopNode.OutputPort);
            var result = GraphModel.CreateTranslator().TranslateAndCompile(GraphModel, AssemblyType.None, CompilationOptions.Tracing);
            Assert.That(result.status, Is.EqualTo(CompilationStatus.Succeeded));
            LogAssert.NoUnexpectedReceived();
        }
    }
}
