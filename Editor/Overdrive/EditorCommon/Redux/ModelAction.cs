using System;
using System.Collections.Generic;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public abstract class ModelAction<ModelType> : BaseAction
    {
        public IReadOnlyList<ModelType> Models;

        protected ModelAction(string undoString)
        {
            UndoString = undoString;
        }

        protected ModelAction(string undoStringSingular, string undoStringPlural, IReadOnlyList<ModelType> models)
        {
            Models = models;
            UndoString = Models == null || Models.Count <= 1 ? undoStringSingular : undoStringPlural;
        }
    }

    public abstract class ModelAction<ModelType, DataType> : ModelAction<ModelType>
    {
        public DataType Value;

        protected ModelAction(string undoString) : base(undoString)
        {
        }

        protected ModelAction(string undoStringSingular, string undoStringPlural,
                              IReadOnlyList<ModelType> models, DataType value)
            : base(undoStringSingular, undoStringPlural, models)
        {
            Value = value;
        }
    }
}
