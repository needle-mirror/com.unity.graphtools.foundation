using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using NUnit.Framework;
using UnityEditor.EditorCommon.Redux;
using UnityEditor.VisualScripting.Editor;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model;
using UnityEditor.VisualScripting.Model.Stencils;
using UnityEngine;

// ReSharper disable AccessToStaticMemberViaDerivedType

namespace UnityEditor.VisualScriptingTests.UI
{
    [SuppressMessage("ReSharper", "ConvertToLocalFunction")]
    class UIPerformanceTests : BaseUIFixture
    {
        protected override bool CreateGraphOnStartup => true;

        static Func<VSGraphModel, int, FunctionModel> MakeDummyFunction =>
            (graphModel, i) => graphModel.CreateFunction("DummyFunction" + i, Vector2.zero);

        static Func<VSGraphModel, int, VariableDeclarationModel> MakeDummyVariableDecl =>
            (graphModel, i) => graphModel.CreateGraphVariableDeclaration("MyVar" + i, typeof(int).GenerateTypeHandle(graphModel.Stencil), true);

        static IEnumerable<object[]> GetEveryActionAffectingTopology()
        {
            var ctx = TestContext.Instance;


            yield return MakeActionTest(new CreateFunctionAction("foo", Vector2.zero));

            var methodInfo = TypeSystem.GetMethod(typeof(Debug), nameof(Debug.Log), true);
            yield return MakeActionTest(new CreateEventFunctionAction(methodInfo, Vector2.zero));

            yield return MakeActionSetup(ctx.FunctionModels, 2, MakeDummyFunction,
                g => new DeleteElementsAction(ctx.FunctionModels[0], ctx.FunctionModels[1]));

            yield return MakeActionSetup(ctx.VariableDeclModels, 1, MakeDummyVariableDecl,
                g => new CreateVariableNodesAction(ctx.VariableDeclModels[0], Vector2.zero));

            yield return MakeActionTest(g => new CreateConstantNodeAction("MyConst", typeof(int).GenerateTypeHandle(g.Stencil), Vector2.zero));

            yield return MakeActionTest(g => new CreateSystemConstantNodeAction("Math > PI", typeof(double).GenerateTypeHandle(g.Stencil),
                typeof(Math).GenerateTypeHandle(g.Stencil), "PI", Vector2.zero));

            yield return MakeEdgeActionSetup(ctx, 1,
                g => new CreateEdgeAction(ctx.InputPorts[0], ctx.OutputPorts[0]));

            yield return MakeActionSetup(ctx.FunctionModels, 1, MakeDummyFunction,
                g => new RenameElementAction(ctx.FunctionModels[0], "newFunctionName"));
        }

        [Test, TestCaseSource(nameof(GetEveryActionAffectingTopology))]
        public void TestPartialRebuild(string testName, State.UIRebuildType rebuildType, Func<VSGraphModel, IAction> getAction)
        {
            var action = getAction(GraphModel);

            Store.Dispatch(new RefreshUIAction(UpdateFlags.All));
            Store.Update();

            State state = Store.GetState(); // save state to watch UI re-building state
            // good enough for tests
#pragma warning disable 612
            Store.DispatchDynamicSlow(action);
#pragma warning restore 612
            Store.Update();

            Assert.That(state.lastActionUIRebuildType, Is.EqualTo(rebuildType));
        }

        static object[] MakeActionTest<T>(T action, State.UIRebuildType rebuildType = State.UIRebuildType.Partial) where T : IAction
        {
            Func<VSGraphModel, IAction> setupFunc = model => action;
            return new object[] { typeof(T).Name, rebuildType, setupFunc};
        }

        static object[] MakeActionTest<T>(Func<VSGraphModel, T> getAction, State.UIRebuildType rebuildType = State.UIRebuildType.Partial) where T : IAction
        {
            return new object[] { typeof(T).Name, rebuildType, getAction };
        }

        static object[] MakeActionSetup<T, TAction>(
            List<T> modelList,
            int numModels,
            Func<VSGraphModel, int, T> makeModel,
            Func<VSGraphModel, TAction> getAction,
            State.UIRebuildType rebuildType = State.UIRebuildType.Partial)
            where T : IGraphElementModel
            where TAction : IAction
        {
            Func<VSGraphModel, TAction> f = graphModel =>
            {
                modelList.Capacity = numModels + 1;
                modelList.Clear();
                for (int i = 0; i < numModels; i++)
                {
                    modelList.Add(makeModel(graphModel, i));
                }

                return getAction(graphModel);
            };
            return new object[] { typeof(T).Name, rebuildType, f };
        }

        static object[] MakeEdgeActionSetup<TAction>(TestContext ctx, int numEdges,
            Func<VSGraphModel, TAction> getAction, State.UIRebuildType rebuildType = State.UIRebuildType.Partial)
            where TAction : IAction
        {
            Func<VSGraphModel, TAction> f = graphModel =>
            {
                ctx.InputPorts.Capacity = numEdges;
                ctx.OutputPorts.Capacity = numEdges;
                ctx.InputPorts.Clear();
                ctx.OutputPorts.Clear();
                for (int i = 0; i < numEdges; i++)
                {
                    ConstantNodeModel c = (ConstantNodeModel)graphModel.CreateConstantNode("Const" + i, typeof(int).GenerateTypeHandle(graphModel.Stencil), Vector2.zero);
                    UnaryOperatorNodeModel op = graphModel.CreateUnaryOperatorNode(UnaryOperatorKind.Minus, Vector2.zero);
                    ctx.InputPorts.Add(op.InputPort as PortModel);
                    ctx.OutputPorts.Add(c.OutputPort as PortModel);
                }

                return getAction(graphModel);
            };
            return new object[] { typeof(TAction).Name, rebuildType, f };
        }
    }
}
