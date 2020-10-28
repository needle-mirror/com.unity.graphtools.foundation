using System;
using NUnit.Framework;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.Actions
{
    [Category("Action")]
    class NodeActionGuidTests : BaseFixture
    {
        protected override bool CreateGraphOnStartup => true;
        protected override Type CreatedGraphType => typeof(ClassStencil);

        class NodeDesc
        {
            public Type Type;
            public GUID Guid;

            public string Name => Type.ToString();
        }

        [Test]
        public void Test_DeleteNodeWithGuid([Values] TestingMode mode)
        {
            var nodes = new[]
            {
                new NodeDesc { Type = typeof(bool) },
                new NodeDesc { Type = typeof(float) },
                new NodeDesc { Type = typeof(Quaternion) },
            };

            foreach (var n in nodes)
            {
                var node = GraphModel.CreateConstantNode(n.Name, n.Type.GenerateTypeHandle(), Vector2.zero);
                n.Guid = node.Guid;

                TestPrereqActionPostreq(mode,
                    () =>
                    {
                        Assert.IsTrue(GraphModel.NodesByGuid.TryGetValue(n.Guid, out var model));
                        return new DeleteElementsAction(new[] { model });
                    },
                    () =>
                    {
                        Assert.IsFalse(GraphModel.NodesByGuid.ContainsKey(n.Guid));
                    });
            }
        }
    }
}
