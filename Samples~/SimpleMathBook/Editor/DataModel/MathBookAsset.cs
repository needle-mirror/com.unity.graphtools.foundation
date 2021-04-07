using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Callbacks;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEditor.GraphToolsFoundation.Overdrive.Samples.MathBook.UI;
using UnityEditor.Searcher;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.MathBook
{
    public class MathBookVariableDeclarationModel : VariableDeclarationModel
    {
    }
    class MathBookStencil : Stencil, ISearcherDatabaseProvider
    {
        SearcherDatabase m_Database;

        List<SearcherDatabase> m_Databases = new List<SearcherDatabase>();

        List<SearcherDatabaseBase> m_BaseDatabases = new List<SearcherDatabaseBase>();

        public override string ToolName
        {
            get { return GraphName; }
        }
        public static string GraphName
        {
            get { return "Math Book"; }
        }

        IGraphElementModel CreateElement(GraphNodeCreationData data, Type nodeType)
        {
            IGraphElementModel model = System.Activator.CreateInstance(nodeType) as IGraphElementModel;

            return model;
        }

        public MathBookStencil()
        {
            var tree = new List<SearcherItem>();

            List<SearcherItem> operators = new List<SearcherItem>();
            operators.Add(new GraphNodeModelSearcherItem(null, t => NodeDataCreationExtensions.CreateNode(t, typeof(MathAdditionOperator)), "Addition"));
            operators.Add(new GraphNodeModelSearcherItem(null, t => NodeDataCreationExtensions.CreateNode(t, typeof(MathSubtractionOperator)), "Subtraction"));
            operators.Add(new GraphNodeModelSearcherItem(null, t => NodeDataCreationExtensions.CreateNode(t, typeof(MathMultiplicationOperator)), "Multiplication"));
            operators.Add(new GraphNodeModelSearcherItem(null, t => NodeDataCreationExtensions.CreateNode(t, typeof(MathDivisionOperator)), "Division"));
            operators.Add(new GraphNodeModelSearcherItem(null, t => NodeDataCreationExtensions.CreateNode(t, typeof(MathResult)), "Result"));

            var operatorsItem = new SearcherItem("Operators", "", operators);


            List<SearcherItem> functions = new List<SearcherItem>();

            functions.Add(new GraphNodeModelSearcherItem(null, t => NodeDataCreationExtensions.CreateNode(t, typeof(SinFunction)), "Sin"));
            functions.Add(new GraphNodeModelSearcherItem(null, t => NodeDataCreationExtensions.CreateNode(t, typeof(AsinFunction)), "Asin"));
            functions.Add(new GraphNodeModelSearcherItem(null, t => NodeDataCreationExtensions.CreateNode(t, typeof(CosFunction)), "Cos"));
            functions.Add(new GraphNodeModelSearcherItem(null, t => NodeDataCreationExtensions.CreateNode(t, typeof(AcosFunction)), "Acos"));
            functions.Add(new GraphNodeModelSearcherItem(null, t => NodeDataCreationExtensions.CreateNode(t, typeof(TanFunction)), "Tan"));
            functions.Add(new GraphNodeModelSearcherItem(null, t => NodeDataCreationExtensions.CreateNode(t, typeof(AtanFunction)), "Atan"));
            functions.Add(new GraphNodeModelSearcherItem(null, t => NodeDataCreationExtensions.CreateNode(t, typeof(MinFunction)), "Min"));
            functions.Add(new GraphNodeModelSearcherItem(null, t => NodeDataCreationExtensions.CreateNode(t, typeof(MaxFunction)), "Max"));
            functions.Add(new GraphNodeModelSearcherItem(null, t => NodeDataCreationExtensions.CreateNode(t, typeof(ClampFunction)), "Clamp"));
            functions.Add(new GraphNodeModelSearcherItem(null, t => NodeDataCreationExtensions.CreateNode(t, typeof(ExpFunction)), "Exp"));
            functions.Add(new GraphNodeModelSearcherItem(null, t => NodeDataCreationExtensions.CreateNode(t, typeof(LogFunction)), "Log"));
            functions.Add(new GraphNodeModelSearcherItem(null, t => NodeDataCreationExtensions.CreateNode(t, typeof(PowFunction)), "Pow"));
            functions.Add(new GraphNodeModelSearcherItem(null, t => NodeDataCreationExtensions.CreateNode(t, typeof(RoundFunction)), "Round"));
            functions.Add(new GraphNodeModelSearcherItem(null, t => NodeDataCreationExtensions.CreateNode(t, typeof(SqrtFunction)), "Sqrt"));

            var functionsItem = new SearcherItem("Functions", "", functions);


            var constants = new List<SearcherItem>();

            constants.Add(new GraphNodeModelSearcherItem(null, t => t.GraphModel.CreateConstantNode(TypeHandle.Float, "", t.Position, t.Guid, null, t.SpawnFlags), "Constant"));
            constants.Add(new GraphNodeModelSearcherItem(null, t => NodeDataCreationExtensions.CreateNode(t, typeof(PIConstant)), "PI"));

            var constantsItem = new SearcherItem("Values", "", constants);

            var items = new SearcherItem[] { operatorsItem, functionsItem, constantsItem };

            m_Database = new SearcherDatabase(items);
            m_Databases.Add(m_Database);
            m_BaseDatabases.Add(m_Database);
        }

        public override Type GetConstantNodeValueType(TypeHandle typeHandle)
        {
            return TypeToConstantMapper.GetConstantNodeType(typeHandle);
        }

        /// <inheritdoc />
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
            return new MathBookBlackboardGraphModel(graphAssetModel);
        }

        public override ISearcherDatabaseProvider GetSearcherDatabaseProvider()
        {
            return this;
        }

        List<SearcherDatabaseBase> ISearcherDatabaseProvider.GetGraphElementsSearcherDatabases(IGraphModel graphModel)
        {
            return m_BaseDatabases;
        }


        List<SearcherDatabase> m_EmptyList = new List<SearcherDatabase>();
        List<SearcherDatabase> ISearcherDatabaseProvider.GetVariableTypesSearcherDatabases()
        {
            return m_EmptyList;
        }

        List<SearcherDatabaseBase> ISearcherDatabaseProvider.GetGraphVariablesSearcherDatabases(IGraphModel graphModel)
        {
            return m_BaseDatabases;
        }

        List<SearcherDatabaseBase> ISearcherDatabaseProvider.GetDynamicSearcherDatabases(IPortModel portModel)
        {
            return m_BaseDatabases;
        }

        public List<SearcherDatabaseBase> GetDynamicSearcherDatabases(IEnumerable<IPortModel> portModel)
        {
            return m_BaseDatabases;
        }
    }

    [Serializable]
    public class MathBookAsset : GraphAssetModel
    {
        protected override Type GraphModelType => typeof(MathBook);

        [MenuItem("Assets/Create/Math Book")]
        public static void CreateGraph(MenuCommand menuCommand)
        {
            const string path = "Assets";
            var template = new GraphTemplate<MathBookStencil>(MathBookStencil.GraphName);
            CommandDispatcher commandDispatcher = null;
            if (EditorWindow.HasOpenInstances<SimpleGraphViewWindow>())
            {
                var window = EditorWindow.GetWindow<SimpleGraphViewWindow>();
                if (window != null)
                {
                    commandDispatcher = window.CommandDispatcher;
                }
            }

            GraphAssetCreationHelpers<MathBookAsset>.CreateInProjectWindow(template, commandDispatcher, path);
        }

        [OnOpenAsset(1)]
        public static bool OpenGraphAsset(int instanceId, int line)
        {
            var obj = EditorUtility.InstanceIDToObject(instanceId);
            if (obj is MathBookAsset)
            {
                string path = AssetDatabase.GetAssetPath(instanceId);
                var asset = AssetDatabase.LoadAssetAtPath<MathBookAsset>(path);
                if (asset == null)
                    return false;

                var window = GraphViewEditorWindow.FindOrCreateGraphWindow<SimpleGraphViewWindow>();
                return window != null;
            }

            return false;
        }

    }
}
