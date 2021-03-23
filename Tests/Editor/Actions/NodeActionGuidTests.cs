using System;
using NUnit.Framework;
using UnityEditor.VisualScripting.Editor;
using UnityEditor.VisualScripting.Model;
using UnityEditor.VisualScripting.Model.Stencils;
using UnityEngine;

namespace UnityEditor.VisualScriptingTests.Actions
{
    [Category("Action")]
    class NodeActionGuidTests : BaseFixture
    {
        protected override bool CreateGraphOnStartup => true;
        protected override Type CreatedGraphType => typeof(ClassStencil);

        struct NodeDesc
        {
            public Type Type;
            public Type ModelType;
            public GUID Guid;

            public string Name => Type.ToString();
        };

        [Test]
        public void Test_CreateNodeWithGuid([Values] TestingMode mode)
        {
            var nodes = new[]
            {
                new NodeDesc { Type = typeof(bool), ModelType = typeof(BooleanConstantNodeModel), Guid = GUID.Generate() },
                new NodeDesc { Type = typeof(float), ModelType = typeof(FloatConstantModel), Guid = GUID.Generate() },
                new NodeDesc { Type = typeof(Quaternion), ModelType = typeof(QuaternionConstantModel), Guid = GUID.Generate() },
            };

            foreach (var n in nodes)
            {
                TestPrereqActionPostreq(mode,
                    () =>
                    {
                        Assert.IsFalse(GraphModel.NodesByGuid.ContainsKey(n.Guid));
                        return new CreateConstantNodeAction(n.Name, n.Type.GenerateTypeHandle(Stencil), Vector2.zero, n.Guid);
                    },
                    () =>
                    {
                        Assert.IsTrue(GraphModel.NodesByGuid.TryGetValue(n.Guid, out var model));
                        Assert.That(model, NUnit.Framework.Is.TypeOf(n.ModelType));
                    });
            }
        }

        [Test]
        public void Test_DeleteNodeWithGuid([Values] TestingMode mode)
        {
            var nodes = new[]
            {
                new NodeDesc { Type = typeof(bool), ModelType = typeof(BooleanConstantNodeModel), Guid = GUID.Generate() },
                new NodeDesc { Type = typeof(float), ModelType = typeof(FloatConstantModel), Guid = GUID.Generate() },
                new NodeDesc { Type = typeof(Quaternion), ModelType = typeof(QuaternionConstantModel), Guid = GUID.Generate() },
            };

            foreach (var n in nodes)
            {
                GraphModel.CreateConstantNode(n.Name, n.Type.GenerateTypeHandle(Stencil), Vector2.zero, guid: n.Guid);
                TestPrereqActionPostreq(mode,
                    () =>
                    {
                        Assert.IsTrue(GraphModel.NodesByGuid.TryGetValue(n.Guid, out var model));
                        return new DeleteElementsAction(model);
                    },
                    () =>
                    {
                        Assert.IsFalse(GraphModel.NodesByGuid.ContainsKey(n.Guid));
                    });
            }
        }
    }
}
