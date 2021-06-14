using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEditor.Searcher;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.MathBook
{
    class MathBookStencil : Stencil, ISearcherDatabaseProvider
    {
        List<SearcherDatabaseBase> m_Databases = new List<SearcherDatabaseBase>();

        public override string ToolName => GraphName;

        public static string GraphName => "Math Book";

        public MathBookStencil()
        {
            SearcherItem MakeSearcherItem((Type t, string name) tuple)
            {
                return new GraphNodeModelSearcherItem(GraphModel, null, data => data.CreateNode(tuple.t), tuple.name);
            }

            var operators = new[]
                {
                    (typeof(MathExpressionNode), "Expression"),
                    (typeof(MathAdditionOperator), "Addition"),
                    (typeof(MathSubtractionOperator), "Subtraction"),
                    (typeof(MathMultiplicationOperator), "Multiplication"),
                    (typeof(MathDivisionOperator), "Division"),
                    (typeof(MathResult), "Result")
                }
                .Select(MakeSearcherItem);
            var operatorsItem = new SearcherItem("Operators", "", operators.ToList());

            var functions = new[]
                {
                    (typeof(SinFunction), "Sin"),
                    (typeof(AsinFunction), "Asin"),
                    (typeof(CosFunction), "Cos"),
                    (typeof(AcosFunction), "Acos"),
                    (typeof(TanFunction), "Tan"),
                    (typeof(AtanFunction), "Atan"),
                    (typeof(MinFunction), "Min"),
                    (typeof(MaxFunction), "Max"),
                    (typeof(ClampFunction), "Clamp"),
                    (typeof(ExpFunction), "Exp"),
                    (typeof(LogFunction), "Log"),
                    (typeof(PowFunction), "Pow"),
                    (typeof(RoundFunction), "Round"),
                    (typeof(SqrtFunction), "Sqrt")
                }
                .Select(MakeSearcherItem);

            var functionsItem = new SearcherItem("Functions", "", functions.ToList());

            var constants = new List<SearcherItem>
            {
                new GraphNodeModelSearcherItem(GraphModel, null,
                    t => t.GraphModel.CreateConstantNode(TypeHandle.Float, "", t.Position, t.Guid, null, t.SpawnFlags),
                    "Constant"),
                MakeSearcherItem((typeof(PIConstant), "PI"))
            };

            var constantsItem = new SearcherItem("Values", "", constants);

            var items = new List<SearcherItem> { operatorsItem, functionsItem, constantsItem };

            var searcherDatabase = new SearcherDatabase(items);
            m_Databases.Add(searcherDatabase);
        }

        public override Type GetConstantNodeValueType(TypeHandle typeHandle)
        {
            return TypeToConstantMapper.GetConstantNodeType(typeHandle);
        }

        public override ISearcherDatabaseProvider GetSearcherDatabaseProvider()
        {
            return this;
        }

        List<SearcherDatabaseBase> ISearcherDatabaseProvider.GetGraphElementsSearcherDatabases(IGraphModel graphModel)
        {
            return m_Databases;
        }

        List<SearcherDatabaseBase> m_EmptyList = new List<SearcherDatabaseBase>();
        List<SearcherDatabaseBase> ISearcherDatabaseProvider.GetVariableTypesSearcherDatabases()
        {
            return m_EmptyList;
        }

        List<SearcherDatabaseBase> ISearcherDatabaseProvider.GetGraphVariablesSearcherDatabases(IGraphModel graphModel)
        {
            return m_Databases;
        }

        List<SearcherDatabaseBase> ISearcherDatabaseProvider.GetDynamicSearcherDatabases(IPortModel portModel)
        {
            return m_Databases;
        }

        public List<SearcherDatabaseBase> GetDynamicSearcherDatabases(IEnumerable<IPortModel> portModel)
        {
            return m_Databases;
        }

        /// <inheritdoc />
        public override IBlackboardGraphModel CreateBlackboardGraphModel(IGraphAssetModel graphAssetModel)
        {
            return new BlackboardGraphModel(graphAssetModel);
        }

        public override void PopulateBlackboardCreateMenu(string sectionName, GenericMenu menu, CommandDispatcher commandDispatcher)
        {
            menu.AddItem(new GUIContent("Create Variable"), false, () =>
            {
                const string newItemName = "variable";
                var finalName = newItemName;
                var i = 0;
                while (commandDispatcher.State.WindowState.GraphModel.VariableDeclarations.Any(v => v.Title == finalName))
                    finalName = newItemName + i++;

                commandDispatcher.Dispatch(new CreateGraphVariableDeclarationCommand(finalName, true, TypeHandle.Float, typeof(MathBookVariableDeclarationModel)));
            });
        }
    }
}
