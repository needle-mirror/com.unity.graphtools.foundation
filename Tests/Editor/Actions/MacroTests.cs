using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.VisualScripting.Editor;
using UnityEditor.VisualScripting.GraphViewModel;
using UnityEditor.VisualScripting.Model;
using UnityEditor.VisualScripting.Model.Stencils;
using UnityEngine;
using UnityEngine.VisualScripting;

// ReSharper disable AccessToStaticMemberViaDerivedType

namespace UnityEditor.VisualScriptingTests.Actions
{
    [Category("Macro")]
    [SuppressMessage("ReSharper", "InlineOutVariableDeclaration")]
    class MacroTests : BaseFixture
    {
        class IO
        {
            IPortModel[] m_Ports;
            int Count => m_Ports.Length;

            public IO(params IPortModel[] ports)
            {
                m_Ports = ports;
            }

            public void Check(VSGraphModel macroGraphModel, IReadOnlyList<IPortModel> macroRefPorts, ModifierFlags modifierFlags)
            {
                Assert.That(macroRefPorts.Count, Is.EqualTo(macroGraphModel.VariableDeclarations.Count(v => v.Modifiers == modifierFlags)));
                Assert.That(macroRefPorts.Count, Is.EqualTo(Count));
                for (int i = 0; i < Count; i++)
                {
                    if (m_Ports[i] == null)
                        Assert.That(macroRefPorts[i].Connected, Is.False);
                    else
                        Assert.That(macroRefPorts[i], Is.ConnectedTo(m_Ports[i]));
                }
            }
        }

        static readonly MethodInfo k_LogMethodInfo = typeof(Debug).GetMethod("Log", new[] { typeof(object) });
        VariableDeclarationModel m_ADecl;
        VariableDeclarationModel m_BDecl;
        VariableDeclarationModel m_CDecl;
        VSGraphModel m_MacroGraphModel;
        protected override bool CreateGraphOnStartup => true;
        protected override Type CreatedGraphType => typeof(ClassStencil);

        public override void SetUp()
        {
            base.SetUp();
            m_ADecl = GraphModel.CreateGraphVariableDeclaration("A", typeof(float).GenerateTypeHandle(Stencil), true);
            m_BDecl = GraphModel.CreateGraphVariableDeclaration("B", typeof(float).GenerateTypeHandle(Stencil), true);
            m_CDecl = GraphModel.CreateGraphVariableDeclaration("C", typeof(float).GenerateTypeHandle(Stencil), true);
            var macroAssetModel = (VSGraphAssetModel)GraphAssetModel.Create("macro", null, typeof(VSGraphAssetModel));
            m_MacroGraphModel = macroAssetModel.CreateVSGraph<MacroStencil>("macro");
        }

        [Test]
        public void MacroPortsRemainConnectedAfterMacroAssetDeletion()
        {
            FunctionCallNodeModel[] log;
            CreateStackAndLogs(out _, out log);
            var binOp = GraphModel.CreateBinaryOperatorNode(BinaryOperatorKind.Add, Vector2.zero);
            var varA = GraphModel.CreateVariableNode(m_ADecl, Vector2.zero);
            GraphModel.CreateEdge(log[0].GetParameterPorts().First(), binOp.OutputPort);
            GraphModel.CreateEdge(binOp.InputPortA, varA.OutputPort);
            GraphModel.CreateEdge(binOp.InputPortB, varA.OutputPort);

            TestPrereqActionPostreq(TestingMode.Action, () =>
            {
                binOp = GraphModel.NodeModels.OfType<BinaryOperatorNodeModel>().Single();
                Assert.That(GraphModel.NodeModels.Count, Is.EqualTo(3));
                Assert.That(GraphModel.NodeModels.OfType<MacroRefNodeModel>().Count(), Is.Zero);
                Assert.That(binOp.InputPortA, Is.ConnectedTo(varA.OutputPort));
                Assert.That(binOp.InputPortB, Is.ConnectedTo(varA.OutputPort));
                Assert.That(binOp.OutputPort, Is.ConnectedTo(log[0].GetParameterPorts().First()));
                return new RefactorExtractMacroAction(new List<IGraphElementModel> { binOp }, Vector2.zero, null);
            }, () =>
                {
                    Assert.That(GraphModel.NodeModels.Count, Is.EqualTo(3));

                    var macroRef = GraphModel.NodeModels.OfType<MacroRefNodeModel>().Single();
                    macroRef.GraphAssetModel = null;
                    macroRef.DefineNode();

                    Assert.That(macroRef, Is.Not.Null);
                    Assert.That(macroRef.InputVariablePorts.First(), Is.ConnectedTo(varA.OutputPort));
                    Assert.That(macroRef.OutputVariablePorts.First(), Is.ConnectedTo(log[0].GetParameterPorts().First()));
                });
        }

        [Test]
        public void ExtractMacroIsUndoable()
        {
            FunctionCallNodeModel[] log;
            CreateStackAndLogs(out _, out log);
            var binOp = GraphModel.CreateBinaryOperatorNode(BinaryOperatorKind.Add, Vector2.zero);
            var varA = GraphModel.CreateVariableNode(m_ADecl, Vector2.zero);
            GraphModel.CreateEdge(log[0].GetParameterPorts().First(), binOp.OutputPort);
            GraphModel.CreateEdge(binOp.InputPortA, varA.OutputPort);
            GraphModel.CreateEdge(binOp.InputPortB, varA.OutputPort);
            Undo.IncrementCurrentGroup();
            TestPrereqActionPostreq(TestingMode.UndoRedo, () =>
            {
                RefreshReference(ref binOp);
                RefreshReference(ref varA);
                RefreshReference(ref log[0]);
                Assert.That(GraphModel.NodeModels.Count, Is.EqualTo(3));
                Assert.That(GraphModel.NodeModels.OfType<MacroRefNodeModel>().Count(), Is.Zero);
                Assert.That(binOp.InputPortA, Is.ConnectedTo(varA.OutputPort));
                Assert.That(binOp.InputPortB, Is.ConnectedTo(varA.OutputPort));
                Assert.That(binOp.OutputPort, Is.ConnectedTo(log[0].GetParameterPorts().First()));
                return new RefactorExtractMacroAction(new List<IGraphElementModel> { binOp }, Vector2.zero, null);
            }, () =>
                {
                    RefreshReference(ref binOp);
                    RefreshReference(ref varA);
                    RefreshReference(ref log[0]);
                    Assert.That(GraphModel.NodeModels.Count, Is.EqualTo(3));
                    var macroRef = GraphModel.NodeModels.OfType<MacroRefNodeModel>().Single();
                    Assert.That(macroRef, Is.Not.Null);
                    Assert.That(macroRef.InputVariablePorts.First(), Is.ConnectedTo(varA.OutputPort));
                    Assert.That(macroRef.OutputVariablePorts.First(), Is.ConnectedTo(log[0].GetParameterPorts().First()));
                });
        }

        [Test]
        public void ExtractTwoNodesConnectedToTheSameNodeDifferentPorts([Values] TestingMode mode)
        {
            var stack = GraphModel.CreateFunction("F", Vector2.zero);
            var set = stack.CreateStackedNode<SetVariableNodeModel>("set");
            var varA = GraphModel.CreateVariableNode(m_ADecl, Vector2.zero);
            var varB = GraphModel.CreateVariableNode(m_BDecl, Vector2.zero);
            GraphModel.CreateEdge(set.InstancePort, varA.OutputPort);
            GraphModel.CreateEdge(set.ValuePort, varB.OutputPort);
            Undo.IncrementCurrentGroup();
            TestPrereqActionPostreq(mode, () =>
            {
                Refresh();
                set = stack.NodeModels.OfType<SetVariableNodeModel>().Single();
                Assert.That(GraphModel.NodeModels.OfType<MacroRefNodeModel>().Count(), Is.Zero);
                Assert.That(set.InstancePort, Is.ConnectedTo(varA.OutputPort));
                Assert.That(set.ValuePort, Is.ConnectedTo(varB.OutputPort));
                return new RefactorExtractMacroAction(new List<IGraphElementModel> { varA, varB }, Vector2.zero, null);
            }, () =>
                {
                    Refresh();
                    var macroRef = GraphModel.NodeModels.OfType<MacroRefNodeModel>().Single();
                    Assert.That(macroRef, Is.Not.Null);
                    var macroRefOutputPorts = macroRef.OutputVariablePorts.ToList();
                    Assert.That(macroRefOutputPorts.Count, Is.EqualTo(2));
                    Assert.That(macroRefOutputPorts[0], Is.ConnectedTo(set.InstancePort));
                    Assert.That(macroRefOutputPorts[1], Is.ConnectedTo(set.ValuePort));
                    Assert.That(macroRef.GraphAssetModel.GraphModel.Stencil, Is.TypeOf<MacroStencil>());
                    Assert.That(((MacroStencil)macroRef.GraphAssetModel.GraphModel.Stencil).ParentType, Is.EqualTo(GraphModel.Stencil.GetType()));
                });

            void Refresh()
            {
                RefreshReference(ref stack);
                RefreshReference(ref set);
                RefreshReference(ref varA);
                RefreshReference(ref varB);
            }
        }

        [Test]
        public void ExtractSticky([Values] TestingMode mode)
        {
            IStickyNoteModel sticky = GraphModel.CreateStickyNote(new Rect()) as StickyNoteModel;
            Undo.IncrementCurrentGroup();

            TestPrereqActionPostreq(mode, () =>
            {
                Refresh();
                Assert.That(GraphModel.NodeModels.OfType<MacroRefNodeModel>().Count(), Is.Zero);
                Assert.That(GraphModel.StickyNoteModels.Count(), Is.EqualTo(1));
                return new RefactorExtractMacroAction(new List<IGraphElementModel> { sticky }, Vector2.zero, null);
            }, () =>
                {
                    Refresh();
                    var macroRef = GraphModel.NodeModels.OfType<MacroRefNodeModel>().Single();
                    Assert.That(macroRef, Is.Not.Null);
                    Assert.That(macroRef.GraphAssetModel.GraphModel.Stencil, Is.TypeOf<MacroStencil>());
                    Assert.That(((VSGraphModel)macroRef.GraphAssetModel.GraphModel).StickyNoteModels.Count(), Is.EqualTo(1));
                    Assert.That(((MacroStencil)macroRef.GraphAssetModel.GraphModel.Stencil).ParentType, Is.EqualTo(GraphModel.Stencil.GetType()));

                    // Assert Sticky has been removed from GraphModel
                    Assert.That(GraphModel.StickyNoteModels.Count(), Is.EqualTo(0));
                });

            void Refresh()
            {
                RefreshReference(ref sticky);
            }
        }

        [Test]
        public void ExtractSingleNode()
        {
            // a + b

            FunctionCallNodeModel[] log;
            CreateStackAndLogs(out _, out log);
            var binOp = GraphModel.CreateBinaryOperatorNode(BinaryOperatorKind.Add, Vector2.zero);
            var varA = GraphModel.CreateVariableNode(m_ADecl, Vector2.zero);
            var varB = GraphModel.CreateVariableNode(m_BDecl, Vector2.zero);
            GraphModel.CreateEdge(log[0].GetParameterPorts().First(), binOp.OutputPort);
            GraphModel.CreateEdge(binOp.InputPortA, varA.OutputPort);
            GraphModel.CreateEdge(binOp.InputPortB, varB.OutputPort);

            TestExtractMacro(new[] { binOp },
                inputs: new IO(varA.OutputPort, varB.OutputPort),
                outputs: new IO(log[0].GetParameterPorts().First()));
        }

        [Test]
        public void ExtractSingleNodeWithSameInputsCreatesOnlyOneMacroInput()
        {
            // a + a

            FunctionCallNodeModel[] log;
            CreateStackAndLogs(out _, out log);
            var binOp = GraphModel.CreateBinaryOperatorNode(BinaryOperatorKind.Add, Vector2.zero);
            var varA = GraphModel.CreateVariableNode(m_ADecl, Vector2.zero);
            GraphModel.CreateEdge(log[0].GetParameterPorts().First(), binOp.OutputPort);
            GraphModel.CreateEdge(binOp.InputPortA, varA.OutputPort);
            GraphModel.CreateEdge(binOp.InputPortB, varA.OutputPort);

            TestExtractMacro(new[] { binOp },
                inputs: new IO(varA.OutputPort),
                outputs: new IO(log[0].GetParameterPorts().First()));
        }

        [Test]
        public void ExtractTwoUnrelatedNodes()
        {
            // a/b and a%b

            FunctionCallNodeModel[] log;
            CreateStackAndLogs(out _, out log, 2);
            var divide = GraphModel.CreateBinaryOperatorNode(BinaryOperatorKind.Divide, Vector2.zero);
            var modulo = GraphModel.CreateBinaryOperatorNode(BinaryOperatorKind.Modulo, Vector2.zero);
            var varA = GraphModel.CreateVariableNode(m_ADecl, Vector2.zero);
            var varB = GraphModel.CreateVariableNode(m_BDecl, Vector2.zero);
            GraphModel.CreateEdge(log[0].GetParameterPorts().First(), divide.OutputPort);
            GraphModel.CreateEdge(log[1].GetParameterPorts().First(), modulo.OutputPort);
            GraphModel.CreateEdge(divide.InputPortA, varA.OutputPort);
            GraphModel.CreateEdge(modulo.InputPortA, varA.OutputPort);
            GraphModel.CreateEdge(divide.InputPortB, varB.OutputPort);
            GraphModel.CreateEdge(modulo.InputPortB, varB.OutputPort);

            TestExtractMacro(new[] { divide, modulo },
                inputs: new IO(varA.OutputPort, varB.OutputPort),
                outputs: new IO(log[0].GetParameterPorts().First(), log[1].GetParameterPorts().First()));
        }

        [Test]
        public void ExtractLinkedThreeNodesWithOneSharedInput()
        {
            // a > b && a < c

            FunctionCallNodeModel[] log;
            CreateStackAndLogs(out _, out log);
            var greater = GraphModel.CreateBinaryOperatorNode(BinaryOperatorKind.GreaterThan, Vector2.zero);
            var lower = GraphModel.CreateBinaryOperatorNode(BinaryOperatorKind.LessThan, Vector2.zero);
            var and = GraphModel.CreateBinaryOperatorNode(BinaryOperatorKind.LogicalAnd, Vector2.zero);
            var varA = GraphModel.CreateVariableNode(m_ADecl, Vector2.zero);
            var varB = GraphModel.CreateVariableNode(m_BDecl, Vector2.zero);
            var varC = GraphModel.CreateVariableNode(m_CDecl, Vector2.zero);

            List<IGraphElementModel> extract = new List<IGraphElementModel>
            {
                greater, lower, and,
            };

            GraphModel.CreateEdge(log[0].GetParameterPorts().First(), and.OutputPort);
            extract.Add(GraphModel.CreateEdge(and.InputPortA, greater.OutputPort));
            extract.Add(GraphModel.CreateEdge(and.InputPortB, lower.OutputPort));

            GraphModel.CreateEdge(greater.InputPortA, varA.OutputPort);
            GraphModel.CreateEdge(greater.InputPortB, varB.OutputPort);

            GraphModel.CreateEdge(lower.InputPortA, varA.OutputPort);
            GraphModel.CreateEdge(lower.InputPortB, varC.OutputPort);

            TestExtractMacro(extract,
                new IO(varA.OutputPort, varB.OutputPort, varC.OutputPort),
                new IO(log[0].GetParameterPorts().First()));
        }

        void TestExtractMacro(IEnumerable<IGraphElementModel> toExtract, IO inputs, IO outputs)
        {
            MacroRefNodeModel macroRef = GraphModel.ExtractNodesAsMacro(m_MacroGraphModel, Vector2.zero, toExtract);

            Assert.That(macroRef.GraphAssetModel.GraphModel, Is.EqualTo(m_MacroGraphModel));

            inputs.Check(m_MacroGraphModel, macroRef.InputVariablePorts.ToList(), ModifierFlags.ReadOnly);
            outputs.Check(m_MacroGraphModel, macroRef.OutputVariablePorts.ToList(), ModifierFlags.WriteOnly);

            CompilationResult compilationResult = GraphModel.Compile(AssemblyType.None, GraphModel.CreateTranslator(), CompilationOptions.Default);
            Assert.That(
                compilationResult.status,
                Is.EqualTo(CompilationStatus.Succeeded));
            Debug.Log(compilationResult.sourceCode[0]);
        }

        void CreateStackAndLogs(out StackBaseModel stack, out FunctionCallNodeModel[] log, int logCount = 1)
        {
            stack = GraphModel.CreateFunction("F", Vector2.zero);
            log = new FunctionCallNodeModel[logCount];
            for (int i = 0; i < logCount; i++)
                log[i] = stack.CreateFunctionCallNode(k_LogMethodInfo);
        }
    }
}
