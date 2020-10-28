using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEngine;

// ReSharper disable AccessToStaticMemberViaDerivedType

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.UI
{
    [SuppressMessage("ReSharper", "ConvertToLocalFunction")]
    class UIPerformanceTests : BaseUIFixture
    {
        protected override bool CreateGraphOnStartup => true;

        static Func<GraphModel, int, Type0FakeNodeModel> MakeDummyFunction =>
            (graphModel, i) => graphModel.CreateNode<Type0FakeNodeModel>("Node" + i, Vector2.zero);

        static Func<GraphModel, int, VariableDeclarationModel> MakeDummyVariableDecl =>
            (graphModel, i) => graphModel.CreateGraphVariableDeclaration("MyVar" + i, typeof(int).GenerateTypeHandle(), ModifierFlags.None, true) as VariableDeclarationModel;

        static IEnumerable<object[]> GetEveryActionAffectingTopology()
        {
            var ctx = TestContext.Instance;

            yield return MakeActionSetup(ctx.Type0FakeNodeModel, 2, MakeDummyFunction,
                g => new DeleteElementsAction(new[] { ctx.Type0FakeNodeModel[0], ctx.Type0FakeNodeModel[1] }));

            yield return MakeActionSetup(ctx.VariableDeclModels, 1, MakeDummyVariableDecl,
                g => new CreateVariableNodesAction(ctx.VariableDeclModels[0], Vector2.zero));

            yield return MakeEdgeActionSetup(ctx, 1, g => new CreateEdgeAction(ctx.InputPorts[0], ctx.OutputPorts[0]));

            yield return MakeActionSetup(ctx.VariableDeclModels, 1, MakeDummyVariableDecl,
                g => new RenameElementAction(ctx.VariableDeclModels[0], "newVariableName"));
        }

        [Test, TestCaseSource(nameof(GetEveryActionAffectingTopology))]
        public void TestPartialRebuild(string testName, State.UIRebuildType rebuildType, Func<TestGraphModel, BaseAction> getAction)
        {
            var action = getAction(GraphModel as TestGraphModel);

            Store.ForceRefreshUI(UpdateFlags.All);;
            Store.Update();

            State state = Store.GetState(); // save state to watch UI re-building state
            Store.Dispatch(action);
            Store.Update();

            Assert.That(state.LastActionUIRebuildType, Is.EqualTo(rebuildType));
        }

        static object[] MakeActionTest<T>(Func<TestGraphModel, T> getAction, State.UIRebuildType rebuildType = State.UIRebuildType.Partial) where T : BaseAction
        {
            return new object[] { typeof(T).Name, rebuildType, getAction };
        }

        static object[] MakeActionSetup<T, TAction>(
            List<T> modelList,
            int numModels,
            Func<GraphModel, int, T> makeModel,
            Func<GraphModel, TAction> getAction,
            State.UIRebuildType rebuildType = State.UIRebuildType.Partial)
            where T : IGraphElementModel
            where TAction : BaseAction
        {
            Func<GraphModel, TAction> f = graphModel =>
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
            Func<GraphModel, TAction> getAction, State.UIRebuildType rebuildType = State.UIRebuildType.Partial)
            where TAction : BaseAction
        {
            Func<GraphModel, TAction> f = graphModel =>
            {
                ctx.InputPorts.Capacity = numEdges;
                ctx.OutputPorts.Capacity = numEdges;
                ctx.InputPorts.Clear();
                ctx.OutputPorts.Clear();
                for (int i = 0; i < numEdges; i++)
                {
                    ConstantNodeModel c = (ConstantNodeModel)graphModel.CreateConstantNode("Const" + i, typeof(int).GenerateTypeHandle(), Vector2.zero);
                    var op = graphModel.CreateNode<Type0FakeNodeModel>("Node0", Vector2.zero);
                    ctx.InputPorts.Add(op.Input0);
                    ctx.OutputPorts.Add(c.OutputPort as PortModel);
                }

                return getAction(graphModel);
            };
            return new object[] { typeof(TAction).Name, rebuildType, f };
        }
    }
}
