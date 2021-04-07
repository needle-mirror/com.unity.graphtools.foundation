using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEditor.Searcher;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.SmartSearch
{
    sealed class ClassForTest { }
    sealed class TypeSearcherDatabaseTests : BaseFixture
    {
        sealed class TestStencil : Stencil
        {
            public static string toolName = "GTF SmartSearch Tests";

            public override string ToolName => toolName;

            public override Type GetConstantNodeValueType(TypeHandle typeHandle)
            {
                return TypeToConstantMapper.GetConstantNodeType(typeHandle);
            }

            public override ISearcherDatabaseProvider GetSearcherDatabaseProvider()
            {
                return new ClassSearcherDatabaseProvider(this);
            }

            public override IGraphProcessingErrorModel CreateProcessingErrorModel(GraphProcessingError error)
            {
                if (error.SourceNode != null && !error.SourceNode.Destroyed)
                {
                    return new GraphProcessingErrorModel(error);
                }

                return null;
            }

            /// <inheritdoc />
            public override IBlackboardGraphModel CreateBlackboardGraphModel(IGraphAssetModel graphAssetModel)
            {
                throw new NotImplementedException();
            }
        }

        Stencil m_Stencil;

        protected override bool CreateGraphOnStartup => false;

        [SetUp]
        public new void SetUp()
        {
            m_Stencil = new TestStencil();
        }

        [Test]
        public void TestClasses()
        {
            var db = TypeSearcherDatabase.FromTypes(m_Stencil, new[] { typeof(string), typeof(ClassForTest) });
            ValidateHierarchy(db.Search("", out _), new[]
            {
                new SearcherItem("Classes", "", new List<SearcherItem>
                {
                    new SearcherItem("System", "", new List<SearcherItem>
                    {
                        new TypeSearcherItem(
                            typeof(string).GenerateTypeHandle(),
                            typeof(string).FriendlyName()
                        )
                    }),
                    new SearcherItem("UnityEditor", "", new List<SearcherItem>
                    {
                        new SearcherItem("GraphToolsFoundation", "", new List<SearcherItem>
                        {
                            new SearcherItem("Overdrive", "", new List<SearcherItem>
                            {
                                new SearcherItem("Tests", "", new List<SearcherItem>
                                {
                                    new SearcherItem("SmartSearch", "", new List<SearcherItem>
                                    {
                                        new TypeSearcherItem(
                                            typeof(ClassForTest).GenerateTypeHandle(),
                                            typeof(ClassForTest).FriendlyName()
                                        )
                                    })
                                })
                            })
                        })
                    })
                })
            });
        }

        static void ValidateHierarchy(IReadOnlyList<SearcherItem> result, IEnumerable<SearcherItem> hierarchy)
        {
            var index = 0;
            TraverseHierarchy(result, hierarchy, ref index);
            Assert.AreEqual(result.Count, index);
        }

        static void TraverseHierarchy(
            IReadOnlyList<SearcherItem> result,
            IEnumerable<SearcherItem> hierarchy,
            ref int index
        )
        {
            foreach (var item in hierarchy)
            {
                Assert.AreEqual(item.Name, result[index].Name);

                if (item.Parent != null)
                    Assert.AreEqual(item.Parent.Name, result[index].Parent.Name);

                index++;

                TraverseHierarchy(result, item.Children, ref index);
            }
        }
    }
}
