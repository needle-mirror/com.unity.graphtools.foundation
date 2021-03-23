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
        List<SearcherDatabaseBase> GetDynamicSearcherDatabases(IEnumerable<IPortModel> portModel);
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
            if (Document == null)
                Document = new List<IIndexableField>();

            if (fieldValue is int i)
            {
                Document.Add(new Int32Field(fieldName, i, Field.Store.NO));
            }
            else if (fieldValue is string s)
            {
                Document.Add(new StringField(fieldName, s, Field.Store.NO));
            }
            else
            {
                throw new NotImplementedException("Only int and string values are indexable at the moment");
            }
        }

        public List<IIndexableField> Document { get; private set; }
    }
}
