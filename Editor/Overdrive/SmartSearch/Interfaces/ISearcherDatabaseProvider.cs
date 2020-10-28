using System;
using System.Collections.Generic;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using UnityEditor.Searcher;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public interface ISearcherDatabaseProvider
    {
        List<SearcherDatabaseBase> GetGraphElementsSearcherDatabases(IGraphModel graphModel);
        List<SearcherDatabase> GetVariableTypesSearcherDatabases();
        List<SearcherDatabaseBase> GetGraphVariablesSearcherDatabases(IGraphModel graphModel);
        List<SearcherDatabaseBase> GetDynamicSearcherDatabases(IPortModel portModel);
    }

    public interface IDocumentIndexer
    {
        void IndexField<T>(string fieldName, T fieldValue);
    }

    public interface IIndexableSearcherDatabaseProvider : ISearcherDatabaseProvider
    {
        bool Index<T>(GraphNodeModelSearcherItem item, IGraphElementModel model, ref T indexer) where T : struct, IDocumentIndexer;
    }

    internal struct DocumentIndexer : IDocumentIndexer
    {
        public void IndexField<T>(string fieldName, T fieldValue)
        {
            if (!(fieldValue is int i))
                throw new NotImplementedException("Only int values are indexable at the moment");
            if (Document == null)
                Document = new List<IIndexableField>();
            Document.Add(new Int32Field(fieldName, i, Field.Store.NO));
        }

        public List<IIndexableField> Document { get; private set; }
    }
}
