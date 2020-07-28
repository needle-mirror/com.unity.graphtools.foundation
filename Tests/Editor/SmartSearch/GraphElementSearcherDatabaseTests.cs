using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using NUnit.Framework;
using UnityEditor.VisualScripting.Editor;
using UnityEditor.VisualScripting.Editor.SmartSearch;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model;
using UnityEditor.VisualScripting.Model.Stencils;
using UnityEngine;
using UnityEngine.VisualScripting;

namespace UnityEditor.VisualScriptingTests.SmartSearch
{
    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    static class GraphElementSearcherDatabaseTestsExtensions
    {
        public static bool DoNothing1(this GraphElementSearcherDatabaseTests o) { return true; }
        internal static int DoNothing2(this GraphElementSearcherDatabaseTests o) { return 0; }
        public static void DoNothing3(this GraphElementSearcherDatabaseTests o) {}
    }

    class GraphElementSearcherDatabaseTests : BaseFixture
    {
        protected override bool CreateGraphOnStartup => true;

#pragma warning disable CS0414
#pragma warning disable CS0649
        [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
        sealed class FakeObject
        {
            public const string Value = "";
            public static readonly string Blah = "";
            public readonly string Details = "";
            public float Num;

            [Obsolete]
            public string ObjName => "FakeObject";

            [Hidden]
            public int Hash => 42;

            public static bool IsActive { get; set; }
            public static int Zero => 0;

            public int this[int index] => index + 1;

            public FakeObject() {}
            public FakeObject(int i) {}

            public string Name => "FakeObject";
            public float GetFloat() { return 1f; }
            public void Foo() {}
        }
#pragma warning restore CS0649
#pragma warning restore CS0414

        void CreateNodesAndValidateGraphModel(GraphNodeModelSearcherItem item, SpawnFlags mode,
            Action<List<INodeModel>> assertNodesCreation)
        {
            var initialNodes = GraphModel.NodeModels.ToList();
            var initialEdges = GraphModel.EdgeModels.ToList();

            item.CreateElements.Invoke(new GraphNodeCreationData(GraphModel, Vector2.zero, mode));

            // If nodes are created as Orphan, graphModel should not be modified
            if (mode.IsOrphan())
            {
                CollectionAssert.AreEqual(initialNodes, GraphModel.NodeModels);
                CollectionAssert.AreEqual(initialEdges, GraphModel.EdgeModels);
                return;
            }

            assertNodesCreation.Invoke(initialNodes);
        }

        void CreateNodesAndValidateStackModel(StackNodeModelSearcherItem item, SpawnFlags mode,
            Action<List<INodeModel>, IStackModel> assertNodesCreation)
        {
            var stack = GraphModel.CreateStack("stack", Vector2.zero);
            var initialGraphNodes = GraphModel.NodeModels.ToList();

            item.CreateElements.Invoke(new StackNodeCreationData(stack, -1, spawnFlags: mode));

            // If nodes are created as Orphan, graphModel and stackModel should not be modified
            if (mode.IsOrphan())
            {
                Assert.AreEqual(stack.NodeModels.Count(), 0);
                CollectionAssert.AreEqual(initialGraphNodes, GraphModel.NodeModels);
                return;
            }

            assertNodesCreation.Invoke(initialGraphNodes, stack);
        }

        [TestCase(SpawnFlags.Default)]
        [TestCase(SpawnFlags.Orphan)]
        public void TestGraphVariables(SpawnFlags mode)
        {
            const string name = "int";
            var var1 = GraphModel.CreateGraphVariableDeclaration(name,
                typeof(int).GenerateTypeHandle(Stencil), false);

            var db = new GraphElementSearcherDatabase(Stencil)
                .AddGraphVariables(GraphModel)
                .Build();

            var results = db.Search("i", out _);
            Assert.AreEqual(1, results.Count);

            var item = (GraphNodeModelSearcherItem)results[0];
            var data = (TypeSearcherItemData)item.Data;
            Assert.AreEqual(SearcherItemTarget.Variable, data.Target);
            Assert.AreEqual(var1.DataType, data.Type);

            CreateNodesAndValidateGraphModel(item, mode, initialNodes =>
            {
                var node = GraphModel.NodeModels.OfType<VariableNodeModel>().FirstOrDefault();
                Assert.IsNotNull(node);
                Assert.AreEqual(initialNodes.Count + 1, GraphModel.NodeModels.Count);
                Assert.AreEqual(name.Nicify(), node.Title);
                Assert.AreEqual(typeof(int), node.DataType.Resolve(Stencil));
            });
        }

        [TestCase("v", typeof(int), SpawnFlags.Default)]
        [TestCase("p", typeof(string), SpawnFlags.Default)]
        [TestCase("p", typeof(string), SpawnFlags.Orphan)]
        public void TestFunctionMembers(string query, Type type, SpawnFlags mode)
        {
            var funcModel = GraphModel.CreateFunction("TestFunc", Vector2.zero);
            funcModel.CreateFunctionVariableDeclaration("var", typeof(int).GenerateTypeHandle(GraphModel.Stencil));
            funcModel.CreateAndRegisterFunctionParameterDeclaration("par", typeof(string).GenerateTypeHandle(GraphModel.Stencil));

            var db = new GraphElementSearcherDatabase(Stencil)
                .AddFunctionMembers(funcModel)
                .Build();

            var results = db.Search(query, out _);
            Assert.AreEqual(1, results.Count);

            var item = (GraphNodeModelSearcherItem)results[0];
            var data = (TypeSearcherItemData)item.Data;
            Assert.AreEqual(SearcherItemTarget.Variable, data.Target);
            Assert.AreEqual(Stencil.GenerateTypeHandle(type), data.Type);

            CreateNodesAndValidateGraphModel(item, mode, initialNodes =>
            {
                var node = GraphModel.NodeModels.OfType<VariableNodeModel>().FirstOrDefault();
                Assert.IsNotNull(node);
                Assert.AreEqual(initialNodes.Count + 1, GraphModel.NodeModels.Count);
                Assert.AreEqual(type, node.DataType.Resolve(Stencil));
            });
        }

        [TestCase(SearcherContext.Stack, "set variable", typeof(SetVariableNodeModel), SpawnFlags.Default)]
        [TestCase(SearcherContext.Stack, "set variable", typeof(SetVariableNodeModel), SpawnFlags.Orphan)]
        public void TestNodesWithSearcherItemAttributes(SearcherContext context, string query, Type type, SpawnFlags mode)
        {
            var db = new GraphElementSearcherDatabase(Stencil)
                .AddNodesWithSearcherItemAttribute()
                .Build();

            var results = db.Search(query, out _);
            var item = (ISearcherItemDataProvider)results[0];
            var data = (NodeSearcherItemData)item.Data;
            Assert.AreEqual(SearcherItemTarget.Node, data.Target);
            Assert.AreEqual(type, data.Type);

            if (context == SearcherContext.Graph)
            {
                Assert.IsTrue(results[0] is GraphNodeModelSearcherItem);
                CreateNodesAndValidateGraphModel((GraphNodeModelSearcherItem)item, mode, initialNodes =>
                {
                    var node = GraphModel.NodeModels.FirstOrDefault(n => n.GetType() == type);
                    Assert.IsNotNull(node);
                    Assert.AreEqual(initialNodes.Count + 1, GraphModel.NodeModels.Count);
                });
            }
            else
            {
                Assert.IsTrue(results[0] is StackNodeModelSearcherItem);
                CreateNodesAndValidateStackModel((StackNodeModelSearcherItem)item, mode, (initialGraphNodes, stack) =>
                {
                    var node = stack.NodeModels.FirstOrDefault(n => n.GetType() == type);
                    Assert.IsNotNull(node);
                    Assert.AreEqual(stack.NodeModels.Count(), 1);
                    Assert.AreEqual(initialGraphNodes.Count, GraphModel.NodeModels.Count);
                });
            }
        }

        [TestCase(SearcherContext.Graph, "in", SearcherItemTarget.InlineExpression)]
        [TestCase(SearcherContext.Graph, "sta", SearcherItemTarget.Stack)]
        [TestCase(SearcherContext.Graph, "new f", SearcherItemTarget.EmptyFunction)]
        [TestCase(SearcherContext.Graph, "sti", SearcherItemTarget.StickyNote)]
        public void TestSingleItem(SearcherContext context, string query, SearcherItemTarget target)
        {
            var db = new GraphElementSearcherDatabase(Stencil)
                .AddInlineExpression()
                .AddStack()
                .AddEmptyFunction()
                .AddStickyNote()
                .Build();

            var results = db.Search(query, out _);

            if (context == SearcherContext.Graph)
                Assert.IsTrue(results[0] is GraphNodeModelSearcherItem);
            else
                Assert.IsTrue(results[0] is StackNodeModelSearcherItem);

            Assert.AreEqual(target, ((ISearcherItemDataProvider)results[0]).Data.Target);
        }

        [Test]
        public void TestAllBinaryOperators()
        {
            var db = new GraphElementSearcherDatabase(Stencil)
                .AddBinaryOperators()
                .Build();

            var results = db.Search("", out _);

            // -1 for the parent item "Operator"
            Assert.AreEqual(Enum.GetValues(typeof(BinaryOperatorKind)).Length, results.Count - 1);

            foreach (var result in results)
            {
                if (result is GraphNodeModelSearcherItem graphItem)
                {
                    Assert.AreEqual(SearcherItemTarget.BinaryOperator, graphItem.Data.Target);
                }
            }
        }

        [TestCase(SpawnFlags.Orphan)]
        [TestCase(SpawnFlags.Default)]
        public void TestBinaryOperator(SpawnFlags mode)
        {
            var db = new GraphElementSearcherDatabase(Stencil)
                .AddBinaryOperators()
                .Build();

            var results = db.Search(nameof(BinaryOperatorKind.Subtract), out _);
            Assert.AreEqual(1, results.Count);

            var item = results[0] as GraphNodeModelSearcherItem;
            Assert.NotNull(item);

            var data = (BinaryOperatorSearcherItemData)item.Data;
            Assert.AreEqual(SearcherItemTarget.BinaryOperator, data.Target);
            Assert.AreEqual(BinaryOperatorKind.Subtract, data.Kind);

            CreateNodesAndValidateGraphModel(item, mode, initialNodes =>
            {
                var node = GraphModel.NodeModels.OfType<BinaryOperatorNodeModel>().FirstOrDefault();
                Assert.IsNotNull(node);
                Assert.AreEqual(node.Kind, BinaryOperatorKind.Subtract);
                Assert.AreEqual(initialNodes.Count + 1, GraphModel.NodeModels.Count);
            });
        }

        [TestCase(SearcherContext.Graph, "mi", UnaryOperatorKind.Minus, SpawnFlags.Default)]
        [TestCase(SearcherContext.Graph, "mi", UnaryOperatorKind.Minus, SpawnFlags.Orphan)]
        [TestCase(SearcherContext.Stack, "post d", UnaryOperatorKind.PostDecrement, SpawnFlags.Default)]
        [TestCase(SearcherContext.Stack, "post D", UnaryOperatorKind.PostDecrement, SpawnFlags.Orphan)]
        [TestCase(SearcherContext.Stack, "post I", UnaryOperatorKind.PostIncrement, SpawnFlags.Default)]
        [TestCase(SearcherContext.Stack, "post I", UnaryOperatorKind.PostIncrement, SpawnFlags.Orphan)]
        [TestCase(SearcherContext.Graph, "lo", UnaryOperatorKind.LogicalNot, SpawnFlags.Default)]
        [TestCase(SearcherContext.Graph, "lo", UnaryOperatorKind.LogicalNot, SpawnFlags.Orphan)]
        public void TestUnaryOperators(SearcherContext context, string query, UnaryOperatorKind kind,
            SpawnFlags mode)
        {
            var db = new GraphElementSearcherDatabase(Stencil)
                .AddUnaryOperators()
                .Build();

            var results = db.Search(query, out _);
            Assert.AreNotEqual(0, results.Count);

            var item = results[0] as ISearcherItemDataProvider;
            Assert.IsNotNull(item);

            var data = (UnaryOperatorSearcherItemData)item.Data;
            Assert.AreEqual(SearcherItemTarget.UnaryOperator, data.Target);
            Assert.AreEqual(kind, data.Kind);

            if (context == SearcherContext.Graph)
            {
                Assert.IsTrue(results[0] is GraphNodeModelSearcherItem);

                CreateNodesAndValidateGraphModel((GraphNodeModelSearcherItem)item, mode, initialNodes =>
                {
                    var node = GraphModel.NodeModels.OfType<UnaryOperatorNodeModel>().FirstOrDefault();
                    Assert.IsNotNull(node);
                    Assert.AreEqual(kind, node.Kind);
                    Assert.AreEqual(initialNodes.Count + 1, GraphModel.NodeModels.Count);
                });
            }
            else
            {
                Assert.IsTrue(results[0] is StackNodeModelSearcherItem);

                CreateNodesAndValidateStackModel((StackNodeModelSearcherItem)item, mode, (initialGraphNodes, stack) =>
                {
                    var node = stack.NodeModels.OfType<UnaryOperatorNodeModel>().FirstOrDefault();
                    Assert.IsNotNull(node);
                    Assert.AreEqual(kind, node.Kind);
                    Assert.AreEqual(1, stack.NodeModels.Count());
                    Assert.AreEqual(initialGraphNodes.Count, GraphModel.NodeModels.Count);
                });
            }
        }

        [TestCase(SearcherContext.Stack, typeof(IfConditionNodeModel), "if", 3, 0)]
        [TestCase(SearcherContext.Stack, typeof(IfConditionNodeModel), "if", 3, 1)]
        [TestCase(SearcherContext.Stack, typeof(IfConditionNodeModel), "if", 3, 2)]
        [TestCase(SearcherContext.Stack, typeof(ForEachHeaderModel), "for each", 1, 0)]
        [TestCase(SearcherContext.Stack, typeof(WhileHeaderModel), "wh", 1, 0)]
        public void TestControlFlows(SearcherContext context, Type loopType, string query, int count, int index)
        {
            var db = new GraphElementSearcherDatabase(Stencil)
                .AddControlFlows()
                .Build();

            var results = db.Search(query, out _);
            Assert.AreEqual(count, results.Count);

            var item = results[index] as ISearcherItemDataProvider;
            Assert.IsNotNull(item);

            var data = (ControlFlowSearcherItemData)item.Data;
            Assert.AreEqual(SearcherItemTarget.ControlFlow, data.Target);
            Assert.AreEqual(loopType, data.Type);

            if (context == SearcherContext.Graph)
                Assert.IsTrue(results[index] is GraphNodeModelSearcherItem);
            else
                Assert.IsTrue(results[index] is StackNodeModelSearcherItem);
        }

        [TestCase(0, SpawnFlags.Default)]
        [TestCase(0, SpawnFlags.Orphan)]
        [TestCase(1, SpawnFlags.Default)]
        [TestCase(1, SpawnFlags.Orphan)]
        [TestCase(2, SpawnFlags.Default)]
        [TestCase(2, SpawnFlags.Orphan)]
        public void TestIfCondition(int index, SpawnFlags mode)
        {
            var db = new GraphElementSearcherDatabase(Stencil)
                .AddControlFlows()
                .Build();

            var results = db.Search("if", out _);
            Assert.AreEqual(3, results.Count);

            var item = results[index] as StackNodeModelSearcherItem;
            Assert.IsNotNull(item);

            var data = (ControlFlowSearcherItemData)item.Data;
            Assert.AreEqual(SearcherItemTarget.ControlFlow, data.Target);
            Assert.AreEqual(typeof(IfConditionNodeModel), data.Type);

            CreateNodesAndValidateStackModel(item, mode, (initialGraphNodes, stack) =>
            {
                var node = stack.NodeModels.OfType<IfConditionNodeModel>().FirstOrDefault();
                Assert.IsNotNull(node);
                Assert.AreEqual(1, stack.NodeModels.Count());

                switch (index)
                {
                    // Basic IfCondition
                    case 0:
                        Assert.AreEqual(initialGraphNodes.Count, GraphModel.NodeModels.Count);
                        break;

                    // Advanced IfCondition
                    case 1:
                        {
                            Assert.AreEqual(initialGraphNodes.Count + 2, GraphModel.NodeModels.Count);
                            Assert.AreEqual(2, GraphModel.EdgeModels.Count);

                            var thenStack = GraphModel.NodeModels.OfType<StackBaseModel>().FirstOrDefault(n => n.Title == "then");
                            Assert.IsNotNull(thenStack);
                            Assert.That(node.ThenPort, Is.ConnectedTo(thenStack.InputPorts[0]));

                            var elseStack = GraphModel.NodeModels.OfType<StackBaseModel>().FirstOrDefault(n => n.Title == "else");
                            Assert.IsNotNull(elseStack);
                            Assert.That(node.ElsePort, Is.ConnectedTo(elseStack.InputPorts[0]));

                            break;
                        }

                    // Complete IfCondition
                    case 2:
                        {
                            Assert.AreEqual(initialGraphNodes.Count + 3, GraphModel.NodeModels.Count);
                            Assert.AreEqual(4, GraphModel.EdgeModels.Count);

                            var thenStack = GraphModel.NodeModels.OfType<StackBaseModel>().FirstOrDefault(n => n.Title == "then");
                            Assert.IsNotNull(thenStack);
                            Assert.That(node.ThenPort, Is.ConnectedTo(thenStack.InputPorts.First()));

                            var elseStack = GraphModel.NodeModels.OfType<StackBaseModel>().FirstOrDefault(n => n.Title == "else");
                            Assert.IsNotNull(elseStack);
                            Assert.That(node.ElsePort, Is.ConnectedTo(elseStack.InputPorts.First()));

                            var lastStack = GraphModel.NodeModels.OfType<IStackModel>().LastOrDefault();
                            Assert.IsNotNull(lastStack);
                            Assert.That(lastStack.InputPorts[0], Is.ConnectedTo(elseStack.OutputPorts[0]));
                            Assert.That(lastStack.InputPorts[0], Is.ConnectedTo(thenStack.OutputPorts[0]));

                            break;
                        }
                }
            });
        }

        [Test]
        public void TestConstants()
        {
            var db = new GraphElementSearcherDatabase(Stencil)
                .AddConstants(new[] { typeof(string) })
                .Build();

            var results = db.Search("st", out _);
            Assert.AreEqual(1, results.Count);

            var item = results[0] as GraphNodeModelSearcherItem;
            Assert.IsNotNull(item);

            var data = (TypeSearcherItemData)item.Data;
            Assert.AreEqual(SearcherItemTarget.Constant, data.Target);
            Assert.AreEqual(typeof(string).GenerateTypeHandle(Stencil), data.Type);
        }

        [TestCase(SearcherContext.Graph, "do", "DoNothing1", 3, 0)]
        [TestCase(SearcherContext.Graph, "do", "DoNothing2", 3, 1)]
        [TestCase(SearcherContext.Stack, "do", "DoNothing3", 3, 2)]
        public void TextExtensionMethods(SearcherContext context, string query, string methodName, int count, int index)
        {
            var db = new GraphElementSearcherDatabase(Stencil)
                .AddExtensionMethods(typeof(GraphElementSearcherDatabaseTests))
                .Build();

            var results = db.Search(query, out _);
            Assert.AreEqual(count, results.Count);

            var item = results[index] as ISearcherItemDataProvider;
            Assert.NotNull(item);

            if (context == SearcherContext.Graph)
                Assert.IsTrue(results[index] is GraphNodeModelSearcherItem);
            else
                Assert.IsTrue(results[index] is StackNodeModelSearcherItem);

            var data = (MethodSearcherItemData)item.Data;
            Assert.AreEqual(SearcherItemTarget.Method, data.Target);
            Assert.AreEqual(methodName, data.MethodInfo.Name);
        }

        [TestCase(1, 0)]
        [TestCase(2, 1)]
        public void TestConstructors(int index, int parameterLength)
        {
            var db = new GraphElementSearcherDatabase(Stencil)
                .AddConstructors(typeof(FakeObject).GetConstructors(BindingFlags.Public | BindingFlags.Instance))
                .Build();

            var results = db.Search("fake", out _);
            Assert.AreEqual(3, results.Count);

            var item = results[index] as GraphNodeModelSearcherItem;
            Assert.NotNull(item);

            var data = (ConstructorSearcherItemData)item.Data;
            Assert.AreEqual(SearcherItemTarget.Constructor, data.Target);
            Assert.AreEqual(parameterLength, data.ConstructorInfo.GetParameters().Length);
        }

        [TestCase(SearcherContext.Graph, "val", "Value", 0, 1, true)]
        [TestCase(SearcherContext.Graph, "blah", "Blah", 0, 1, true)]
        [TestCase(SearcherContext.Graph, "de", "Details", 0, 1, false)]
        [TestCase(SearcherContext.Graph, "nu", "Num", 0, 2, false)]
        [TestCase(SearcherContext.Stack, "nu", "Num", 1, 2, false)]
        public void TestFields(SearcherContext context, string query, string fieldName, int index, int count,
            bool isConstant)
        {
            var db = new GraphElementSearcherDatabase(Stencil)
                .AddFields(typeof(FakeObject).GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
                .Build();

            var results = db.Search(query, out _);
            Assert.AreEqual(count, results.Count);

            var item = results[index] as ISearcherItemDataProvider;
            Assert.NotNull(item);

            if (context == SearcherContext.Graph)
            {
                Assert.IsTrue(results[index] is GraphNodeModelSearcherItem);

                var graphItem = (GraphNodeModelSearcherItem)results[index];
                var node = graphItem.CreateElements.Invoke(new GraphNodeCreationData(GraphModel, Vector2.down)).First();
                Assert.That(node is ISystemConstantNodeModel, NUnit.Framework.Is.EqualTo(isConstant));
            }
            else
            {
                Assert.IsTrue(results[index] is StackNodeModelSearcherItem);
            }

            var data = (FieldSearcherItemData)item.Data;
            Assert.AreEqual(SearcherItemTarget.Field, data.Target);
            Assert.AreEqual(fieldName, data.FieldInfo.Name);
        }

        [TestCase(SearcherContext.Graph, "item", "Item", 0, 1)]
        [TestCase(SearcherContext.Graph, "is", "IsActive", 0, 2)]
        [TestCase(SearcherContext.Stack, "is", "IsActive", 1, 2)]
        [TestCase(SearcherContext.Graph, "ze", "Zero", 0, 1)]
        [TestCase(SearcherContext.Graph, "nam", "Name", 0, 1)]
        public void TestProperties(SearcherContext context, string query, string propertyName, int index, int count)
        {
            var db = new GraphElementSearcherDatabase(Stencil)
                .AddProperties(typeof(FakeObject).GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
                .Build();

            var results = db.Search(query, out _);
            Assert.AreEqual(count, results.Count);

            var item = results[index] as ISearcherItemDataProvider;
            Assert.NotNull(item);

            if (context == SearcherContext.Graph)
                Assert.IsTrue(results[index] is GraphNodeModelSearcherItem);
            else
                Assert.IsTrue(results[index] is StackNodeModelSearcherItem);

            var data = (PropertySearcherItemData)item.Data;
            Assert.AreEqual(SearcherItemTarget.Property, data.Target);
            Assert.AreEqual(propertyName, data.PropertyInfo.Name);
        }

        [TestCase(SearcherContext.Graph, "float", "GetFloat")]
        [TestCase(SearcherContext.Stack, "foo", "Foo")]
        public void TestMethods(SearcherContext context, string query, string methodName)
        {
            var db = new GraphElementSearcherDatabase(Stencil)
                .AddMethods(typeof(FakeObject).GetMethods(BindingFlags.Public | BindingFlags.Instance))
                .Build();

            var results = db.Search(query, out _);
            Assert.AreEqual(1, results.Count);

            var item = results[0] as ISearcherItemDataProvider;
            Assert.NotNull(item);

            if (context == SearcherContext.Graph)
                Assert.IsTrue(results[0] is GraphNodeModelSearcherItem);
            else
                Assert.IsTrue(results[0] is StackNodeModelSearcherItem);

            var data = (MethodSearcherItemData)item.Data;
            Assert.AreEqual(SearcherItemTarget.Method, data.Target);
            Assert.AreEqual(methodName, data.MethodInfo.Name);
        }

        [Test]
        public void TestGetMacros()
        {
            const string graphName = "TestGraphElement_GetMacros";
            const string path = "Assets/" + graphName + ".asset";
            m_Store.Dispatch(new CreateGraphAssetAction(typeof(MacroStencil), "TestMacro", path));

            var a = new GraphElementSearcherDatabase(Stencil);
            var b = a.AddMacros();
            b.Build();

            var db = new GraphElementSearcherDatabase(Stencil)
                .AddMacros()
                .Build();

            var result = db.Search(graphName, out _).OfType<GraphNodeModelSearcherItem>().ToList();
            Assert.AreEqual(1, result.Count);

            var item = (GraphNodeModelSearcherItem)result[0];
            Assert.IsNotNull(item);
            Assert.AreEqual("Macros", item.Parent.Name);

            AssetDatabase.DeleteAsset(path);
        }
    }
}
