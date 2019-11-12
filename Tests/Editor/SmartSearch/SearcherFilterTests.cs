using System;
using System.Collections.Generic;
using System.Reflection;
using JetBrains.Annotations;
using Moq;
using NUnit.Framework;
using UnityEditor.VisualScripting.Editor.SmartSearch;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model;
using UnityEditor.VisualScripting.Model.Stencils;
using UnityEngine;

namespace UnityEditor.VisualScriptingTests.SmartSearch
{
    class SearcherFilterTests : BaseFixture
    {
        protected override bool CreateGraphOnStartup => true;

        internal abstract class BaseFakeObject {}

#pragma warning disable CS0414
#pragma warning disable CS0649
        [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
        internal class FakeObject : BaseFakeObject
        {
            public readonly FakeObject Child;
            public int Index = 1;
            public static int Max = 10;

            public string Name { get; }
            public static int Zero => 0;

            [UsedImplicitly]
            public FakeObject() {}

            public void Foo() {}
            public FakeObject GetChild() { return Child; }

            public static bool operator!(FakeObject fo) { return false; }
        }
#pragma warning restore CS0649
#pragma warning restore CS0414

        [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
        internal class OtherFakeObject
        {
            public static void Bar(FakeObject fo) {}

            public void DoStuff() {}
        }

        [UsedImplicitly]
        static IEnumerable<TestCaseData> WithFunctionRefsTestCaseData
        {
            get
            {
                yield return new TestCaseData(new Mock<IVSGraphModel>().Object, new Mock<IFunctionModel>().Object, true);
                yield return new TestCaseData(new Mock<IVSGraphModel>().Object, null, false);
            }
        }

        [UsedImplicitly]
        static IEnumerable<TestCaseData> WithGraphAssetsTestCaseData
        {
            get
            {
                yield return new TestCaseData(new Mock<IGraphAssetModel>().Object, true);
                yield return new TestCaseData(null, false);
            }
        }

        [UsedImplicitly]
        static IEnumerable<TestCaseData> WithMethodsTestCaseData
        {
            get
            {
                yield return new TestCaseData(new Mock<MethodInfo>().Object, true);
                yield return new TestCaseData(null, false);
            }
        }

        [TestCase(typeof(bool), typeof(IfConditionNodeModel), true, true)]
        [TestCase(typeof(int), typeof(IfConditionNodeModel), true, false)]
        [TestCase(typeof(bool), typeof(ForEachHeaderModel), true, false)]
        [TestCase(typeof(bool), typeof(IfConditionNodeModel), false, false)]
        [TestCase(typeof(int), typeof(ForEachHeaderModel), true, false)]
        [TestCase(typeof(int), typeof(ForEachHeaderModel), false, false)]
        [TestCase(typeof(bool), typeof(ForEachHeaderModel), false, false)]
        [TestCase(typeof(int), typeof(IfConditionNodeModel), false, false)]
        public void TestWithIfConditions(Type inputType, Type controlFlowType, bool acceptNode, bool result)
        {
            var stackMock = new Mock<IStackModel>();
            stackMock.Setup(s => s.AcceptNode(It.IsAny<Type>())).Returns(acceptNode);

            var th = Stencil.GenerateTypeHandle(inputType);
            var filter = new SearcherFilter(SearcherContext.Stack).WithIfConditions(th, stackMock.Object);
            var data = new ControlFlowSearcherItemData(controlFlowType);

            Assert.AreEqual(result, filter.ApplyFilters(data));
        }

        [TestCase(typeof(int), true, true)]
        [TestCase(typeof(int), false, false)]
        [TestCase(null, true, false)]
        [TestCase(null, false, false)]
        public void TestWithControlFlowsInStack(Type nodeType, bool acceptNode, bool result)
        {
            var stackMock = new Mock<IStackModel>();
            stackMock.Setup(s => s.AcceptNode(It.IsAny<Type>())).Returns(acceptNode);

            var filter = new SearcherFilter(SearcherContext.Stack).WithControlFlows(stackMock.Object);
            var data = new ControlFlowSearcherItemData(nodeType);

            Assert.AreEqual(result, filter.ApplyFilters(data));
        }

        [TestCase(typeof(IfConditionNodeModel), true)]
        [TestCase(null, false)]
        public void TestWithControlFlows(Type type, bool result)
        {
            var filter = new SearcherFilter(SearcherContext.Graph).WithControlFlows();
            var data = new ControlFlowSearcherItemData(type);

            Assert.AreEqual(result, filter.ApplyFilters(data));
        }

        [TestCase(typeof(IfConditionNodeModel), null, false)]
        [TestCase(typeof(IfConditionNodeModel), typeof(IfConditionNodeModel), true)]
        [TestCase(typeof(WhileNodeModel), typeof(IfConditionNodeModel), false)]
        public void TestWithControlFlowOfType(Type filterType, Type returnType, bool result)
        {
            var filter = new SearcherFilter(SearcherContext.Graph).WithControlFlow(filterType);
            var data = new ControlFlowSearcherItemData(returnType);

            Assert.AreEqual(result, filter.ApplyFilters(data));
        }

        [TestCase(typeof(IfConditionNodeModel), null, true, false)]
        [TestCase(typeof(IfConditionNodeModel), null, false, false)]
        [TestCase(typeof(IfConditionNodeModel), typeof(IfConditionNodeModel), true, true)]
        [TestCase(typeof(IfConditionNodeModel), typeof(IfConditionNodeModel), false, false)]
        [TestCase(typeof(WhileNodeModel), typeof(IfConditionNodeModel), true, false)]
        [TestCase(typeof(WhileNodeModel), typeof(IfConditionNodeModel), false, false)]
        public void TestWithControlFlowOfTypeInStack(Type filterType, Type returnType, bool acceptNode, bool result)
        {
            var stackMock = new Mock<IStackModel>();
            stackMock.Setup(s => s.AcceptNode(It.IsAny<Type>())).Returns(acceptNode);

            var filter = new SearcherFilter(SearcherContext.Stack).WithControlFlow(filterType, stackMock.Object);
            var data = new ControlFlowSearcherItemData(returnType);

            Assert.AreEqual(result, filter.ApplyFilters(data));
        }

        [TestCaseSource(nameof(WithMethodsTestCaseData))]
        public void TestWithMethods(MethodInfo methodInfo, bool result)
        {
            var filter = new SearcherFilter(SearcherContext.Graph).WithMethods();
            var data = new MethodSearcherItemData(methodInfo);

            Assert.AreEqual(result, filter.ApplyFilters(data));
        }

        [TestCaseSource(nameof(WithGraphAssetsTestCaseData))]
        public void TestWithGraphAssets(IGraphAssetModel graphAssetModel, bool result)
        {
            var filter = new SearcherFilter(SearcherContext.Graph).WithMacros();
            var data = new GraphAssetSearcherItemData(graphAssetModel);

            Assert.AreEqual(result, filter.ApplyFilters(data));
        }

        [TestCase(typeof(Unknown), typeof(string), true)]
        [TestCase(typeof(Transform), typeof(Component), false)]
        [TestCase(typeof(Component), typeof(Transform), true)]
        public void TestWithVariables(Type portDataType, Type variableType, bool result)
        {
            var portMock = new Mock<IPortModel>();
            portMock.Setup(p => p.DataType).Returns(Stencil.GenerateTypeHandle(portDataType));
            portMock.Setup(p => p.NodeModel).Returns(new Mock<INodeModel>().Object);

            var filter = new SearcherFilter(SearcherContext.Graph).WithVariables(Stencil, portMock.Object);
            var data = new TypeSearcherItemData(Stencil.GenerateTypeHandle(variableType), SearcherItemTarget.Variable);


            Assert.AreEqual(result, filter.ApplyFilters(data));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void TestWithVariables_OnIOperationValidatorNode(bool expectedResult)
        {
            var dataType = Stencil.GenerateTypeHandle(typeof(bool));

            var portMock = new Mock<IPortModel>();
            portMock.Setup(p => p.DataType).Returns(dataType);

            var nodeMock = new Mock<UnaryOperatorNodeModel>();
            nodeMock.Setup(p => p.HasValidOperationForInput(portMock.Object, dataType)).Returns(expectedResult);
            portMock.Setup(p => p.NodeModel).Returns(nodeMock.Object);

            var filter = new SearcherFilter(SearcherContext.Graph).WithVariables(Stencil, portMock.Object);
            var data = new TypeSearcherItemData(dataType, SearcherItemTarget.Variable);


            Assert.AreEqual(expectedResult, filter.ApplyFilters(data));
        }

        [TestCase(typeof(SetVariableNodeModel), true, true)]
        [TestCase(typeof(SetVariableNodeModel), false, false)]
        [TestCase(typeof(SetPropertyGroupNodeModel), true, false)]
        [TestCase(typeof(SetPropertyGroupNodeModel), false, false)]
        public void TestWithVisualScriptingNodesOfTypeInStack(Type nodeType, bool acceptNode, bool result)
        {
            var stackMock = new Mock<IStackModel>();
            stackMock.Setup(s => s.AcceptNode(It.IsAny<Type>())).Returns(acceptNode);

            var filter = new SearcherFilter(SearcherContext.Stack).WithVisualScriptingNodes(nodeType, stackMock.Object);
            var data = new NodeSearcherItemData(typeof(SetVariableNodeModel));

            Assert.AreEqual(result, filter.ApplyFilters(data));
        }

        [TestCase(typeof(int), true, true)]
        [TestCase(typeof(int), false, false)]
        [TestCase(null, true, false)]
        public void TestWithVisualScriptingNodesInStack(Type nodeType, bool acceptNode, bool result)
        {
            var stackMock = new Mock<IStackModel>();
            stackMock.Setup(s => s.AcceptNode(It.IsAny<Type>())).Returns(acceptNode);

            new SearcherFilter(SearcherContext.Graph).WithVisualScriptingNodes(stackMock.Object);
        }

        [TestCase(typeof(IEventFunctionModel), typeof(KeyDownEventModel), false)]
        [TestCase(typeof(ThisNodeModel), typeof(ThisNodeModel), false)]
        [TestCase(typeof(int), typeof(string), true)]
        [TestCase(typeof(string), typeof(int), true)]
        [TestCase(typeof(string), null, false)]
        public void TestWithVisualScriptingNodesExcept(Type exception, Type dataType, bool result)
        {
            var filter = new SearcherFilter(SearcherContext.Graph).WithVisualScriptingNodesExcept(new[] { exception });
            var data = new NodeSearcherItemData(dataType);

            Assert.AreEqual(result, filter.ApplyFilters(data));
        }

        [TestCaseSource(nameof(WithFunctionRefsTestCaseData))]
        public void TestFunctionRefs(IGraphModel graph, IFunctionModel functionModel, bool result)
        {
            var filter = new SearcherFilter(SearcherContext.Graph).WithFunctionReferences();
            var data = new FunctionRefSearcherItemData(graph, functionModel);

            Assert.AreEqual(result, filter.ApplyFilters(data));
        }

        [Test]
        public void TestVisualScriptingNodes()
        {
            var filter = new SearcherFilter(SearcherContext.Graph).WithVisualScriptingNodes();
            var data = new NodeSearcherItemData(typeof(SetVariableNodeModel));

            Assert.IsTrue(filter.ApplyFilters(data));
        }

        [TestCase(typeof(SetVariableNodeModel), true)]
        [TestCase(typeof(SetPropertyGroupNodeModel), false)]
        public void TestVisualScriptingNodesOfType(Type type, bool result)
        {
            var filter = new SearcherFilter(SearcherContext.Graph).WithVisualScriptingNodes(type);
            var data = new NodeSearcherItemData(typeof(SetVariableNodeModel));

            Assert.AreEqual(result, filter.ApplyFilters(data));
        }

        [Test]
        public void TestWithStickyNote()
        {
            var filter = new SearcherFilter(SearcherContext.Graph).WithStickyNote();
            var data = new SearcherItemData(SearcherItemTarget.StickyNote);

            Assert.IsTrue(filter.ApplyFilters(data));
        }

        [Test]
        public void TestWithEmptyFunction()
        {
            var filter = new SearcherFilter(SearcherContext.Graph).WithEmptyFunction();
            var data = new SearcherItemData(SearcherItemTarget.EmptyFunction);

            Assert.IsTrue(filter.ApplyFilters(data));
        }

        [Test]
        public void TestWithStack()
        {
            var filter = new SearcherFilter(SearcherContext.Graph).WithStack();
            var data = new SearcherItemData(SearcherItemTarget.Stack);

            Assert.IsTrue(filter.ApplyFilters(data));
        }

        [Test]
        public void TestWithInlineExpression()
        {
            var filter = new SearcherFilter(SearcherContext.Graph).WithInlineExpression();
            var data = new SearcherItemData(SearcherItemTarget.InlineExpression);

            Assert.IsTrue(filter.ApplyFilters(data));
        }

        [Test]
        public void TestWithBinaryOperators()
        {
            var filter = new SearcherFilter(SearcherContext.Graph).WithBinaryOperators();
            var data = new BinaryOperatorSearcherItemData(BinaryOperatorKind.Add);

            Assert.IsTrue(filter.ApplyFilters(data));
        }

        [TestCase(typeof(FakeObject), UnaryOperatorKind.LogicalNot, false, true)]
        [TestCase(typeof(FakeObject), UnaryOperatorKind.LogicalNot, true, false)]
        [TestCase(typeof(FakeObject), UnaryOperatorKind.Minus, false, false)]
        public void TestWithUnaryOperatorsForType(Type type, UnaryOperatorKind kind, bool isConstant, bool result)
        {
            var filter = new SearcherFilter(SearcherContext.Graph).WithUnaryOperators(type, isConstant);
            var data = new UnaryOperatorSearcherItemData(kind);

            Assert.AreEqual(result, filter.ApplyFilters(data));
        }

        [Test]
        public void TestWithUnaryOperators()
        {
            var filter = new SearcherFilter(SearcherContext.Graph).WithUnaryOperators();
            var data = new UnaryOperatorSearcherItemData(UnaryOperatorKind.Minus);

            Assert.IsTrue(filter.ApplyFilters(data));
        }

        [TestCase(typeof(string), typeof(string), true)]
        [TestCase(typeof(string), typeof(object), true)]
        [TestCase(typeof(object), typeof(string), false)]
        [TestCase(typeof(int), typeof(string), false)]
        [TestCase(typeof(int), typeof(Unknown), true)]
        public void TestWithConstantsOfType(Type constType, Type portDataType, bool result)
        {
            var portMock = new Mock<IPortModel>();
            portMock.Setup(p => p.DataType).Returns(Stencil.GenerateTypeHandle(portDataType));

            var filter = new SearcherFilter(SearcherContext.Graph).WithConstants(Stencil, portMock.Object);
            var data = new TypeSearcherItemData(Stencil.GenerateTypeHandle(constType), SearcherItemTarget.Constant);

            Assert.AreEqual(result, filter.ApplyFilters(data));
        }

        [Test]
        public void TestWithConstants()
        {
            var filter = new SearcherFilter(SearcherContext.Graph).WithConstants();
            var data = new TypeSearcherItemData(Stencil.GenerateTypeHandle(typeof(string)), SearcherItemTarget.Constant);

            Assert.IsTrue(filter.ApplyFilters(data));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void TestWithConstants_OnIOperationValidatorNode(bool expectedResult)
        {
            var dataType = Stencil.GenerateTypeHandle(typeof(bool));

            var portMock = new Mock<IPortModel>();
            portMock.Setup(p => p.DataType).Returns(dataType);

            var nodeMock = new Mock<UnaryOperatorNodeModel>();
            nodeMock.Setup(p => p.HasValidOperationForInput(portMock.Object, dataType)).Returns(expectedResult);
            portMock.Setup(p => p.NodeModel).Returns(nodeMock.Object);

            var filter = new SearcherFilter(SearcherContext.Graph).WithConstants(Stencil, portMock.Object);
            var data = new TypeSearcherItemData(dataType, SearcherItemTarget.Constant);


            Assert.AreEqual(expectedResult, filter.ApplyFilters(data));
        }

        [TestCase("GetChild", typeof(FakeObject), typeof(FakeObject), true)]
        [TestCase("GetChild", typeof(FakeObject), typeof(BaseFakeObject), true)]
        [TestCase("GetChild", typeof(FakeObject), typeof(string), false)]
        public void TestWithMethodsOfDeclaringAndReturnTypes(string methodName, Type declaringType, Type returnType,
            bool result)
        {
            var filter = new SearcherFilter(SearcherContext.Graph).WithMethods(declaringType, returnType);
            var data = new MethodSearcherItemData(typeof(FakeObject).GetMethod(methodName));

            Assert.AreEqual(result, filter.ApplyFilters(data));
        }

        [TestCase(typeof(FakeObject), "Foo", true)]
        [TestCase(typeof(OtherFakeObject), "Bar", true)]
        [TestCase(typeof(OtherFakeObject), "DoStuff", false)]
        public void TestWithMethodsOfType(Type type, string name, bool result)
        {
            var filter = new SearcherFilter(SearcherContext.Graph).WithMethods(typeof(FakeObject));
            var data = new MethodSearcherItemData(type.GetMethod(name));

            Assert.AreEqual(result, filter.ApplyFilters(data));
        }

        [TestCase("Foo", true)]
        [TestCase("", false)]
        public void TestWithMethods(string name, bool result)
        {
            var filter = new SearcherFilter(SearcherContext.Graph).WithMethods();
            var data = new MethodSearcherItemData(typeof(FakeObject).GetMethod(name));

            Assert.AreEqual(result, filter.ApplyFilters(data));
        }

        [TestCase("Index", true)]
        [TestCase("Max", false)]
        [TestCase("", false)]
        public void TestWithFields(string name, bool result)
        {
            var filter = new SearcherFilter(SearcherContext.Graph).WithFields(typeof(FakeObject));
            var data = new FieldSearcherItemData(typeof(FakeObject).GetField(name));

            Assert.AreEqual(result, filter.ApplyFilters(data));
        }

        [TestCase("Child", typeof(FakeObject), typeof(FakeObject), true)]
        [TestCase("Child", typeof(FakeObject), typeof(BaseFakeObject), true)]
        [TestCase("Child", typeof(FakeObject), typeof(string), false)]
        public void TestWithFieldsOfDeclaringAndFieldTypes(string fieldName, Type declaringType, Type fieldType,
            bool result)
        {
            var filter = new SearcherFilter(SearcherContext.Graph).WithFields(declaringType, fieldType);
            var data = new FieldSearcherItemData(typeof(FakeObject).GetField(fieldName));

            Assert.AreEqual(result, filter.ApplyFilters(data));
        }

        [Test]
        public void TestWithConstructors()
        {
            var filter = new SearcherFilter(SearcherContext.Graph).WithConstructors();
            var data = new ConstructorSearcherItemData(typeof(FakeObject).GetConstructors()[0]);

            Assert.IsTrue(filter.ApplyFilters(data));

            data = new ConstructorSearcherItemData(null);
            Assert.IsFalse(filter.ApplyFilters(data));
        }

        [TestCase("Name", true)]
        [TestCase("", false)]
        public void TestWithProperties(string name, bool result)
        {
            var filter = new SearcherFilter(SearcherContext.Graph).WithProperties();
            var data = new PropertySearcherItemData(typeof(FakeObject).GetProperty(name));

            Assert.AreEqual(result, filter.ApplyFilters(data));
        }

        [TestCase(typeof(FakeObject), "Name", true)]
        [TestCase(typeof(string), "Name", false)]
        public void TestWithPropertiesOfType(Type type, string name, bool result)
        {
            var filter = new SearcherFilter(SearcherContext.Graph).WithProperties(type);
            var data = new PropertySearcherItemData(typeof(FakeObject).GetProperty(name));

            Assert.AreEqual(result, filter.ApplyFilters(data));
        }

        [TestCase("Name", typeof(FakeObject), typeof(string), true, true)]
        [TestCase("Name", typeof(FakeObject), typeof(object), true, true)]
        [TestCase("Name", typeof(FakeObject), typeof(int), true, false)]
        [TestCase("Zero", typeof(FakeObject), typeof(int), true, true)]
        [TestCase("Zero", typeof(FakeObject), typeof(int), false, false)]
        public void TestWithPropertiesOfDeclaringAndReturnTypes(string propertyName, Type declaringType,
            Type returnType, bool allowConstant, bool result)
        {
            var filter = new SearcherFilter(SearcherContext.Graph).WithProperties(declaringType, returnType, allowConstant);
            var data = new PropertySearcherItemData(typeof(FakeObject).GetProperty(propertyName));

            Assert.AreEqual(result, filter.ApplyFilters(data));
        }

        [TestCase(typeof(TestingMode), true)]
        [TestCase(typeof(SearcherFilter), false)]
        public void TestWithEnums(Type type, bool expectedResult)
        {
            var filter = new SearcherFilter(SearcherContext.Type).WithEnums(Stencil);
            var data = new TypeSearcherItemData(type.GenerateTypeHandle(Stencil), SearcherItemTarget.Type);

            Assert.AreEqual(expectedResult, filter.ApplyFilters(data));
        }

        class TestFilter : SearcherFilter
        {
            public TestFilter(SearcherContext context)
                : base(context) {}

            internal TestFilter WithString()
            {
                this.RegisterType(d => d.Type == TypeHandle.String);
                return this;
            }

            internal TestFilter WithInt()
            {
                this.RegisterType(d => d.Type == TypeHandle.Int);
                return this;
            }
        }

        [TestCase(typeof(string), true)]
        [TestCase(typeof(int), true)]
        [TestCase(typeof(float), false)]
        public void TestMultipleFilters(Type type, bool expectedResult)
        {
            var filter = new TestFilter(SearcherContext.Type).WithInt().WithString();
            var data = new TypeSearcherItemData(type.GenerateTypeHandle(Stencil), SearcherItemTarget.Type);

            Assert.AreEqual(expectedResult, filter.ApplyFilters(data));
        }
    }
}
