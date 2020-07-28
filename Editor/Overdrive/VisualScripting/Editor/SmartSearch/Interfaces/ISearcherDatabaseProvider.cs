using System;
using System.Collections.Generic;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;
using UnityEditor.Searcher;

namespace UnityEditor.GraphToolsFoundation.Overdrive.VisualScripting.SmartSearch
{
    public interface ISearcherDatabaseProvider
    {
        List<SearcherDatabaseBase> GetGraphElementsSearcherDatabases(IGTFGraphModel graphModel);
        List<SearcherDatabase> GetVariableTypesSearcherDatabases();
        List<SearcherDatabaseBase> GetGraphVariablesSearcherDatabases(IGTFGraphModel graphModel);
        List<SearcherDatabaseBase> GetDynamicSearcherDatabases(IGTFPortModel portModel);
    }

    public interface IDocumentIndexer
    {
        void IndexField<T>(string fieldName, T fieldValue);
    }

    public interface IIndexableSearcherDatabaseProvider : ISearcherDatabaseProvider
    {
        bool Index<T>(GraphNodeModelSearcherItem item, IGTFGraphElementModel model, ref T indexer) where T : struct, IDocumentIndexer;
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
