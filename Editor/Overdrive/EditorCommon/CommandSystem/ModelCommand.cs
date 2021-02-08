using System;
using System.Collections.Generic;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public abstract class ModelCommand<ModelType> : Command
    {
        public IReadOnlyList<ModelType> Models;

        protected ModelCommand(string undoString)
        {
            UndoString = undoString;
        }

        protected ModelCommand(string undoStringSingular, string undoStringPlural, IReadOnlyList<ModelType> models)
        {
            Models = models;
            UndoString = Models == null || Models.Count <= 1 ? undoStringSingular : undoStringPlural;
        }
    }

    public abstract class ModelCommand<ModelType, DataType> : ModelCommand<ModelType>
    {
        public DataType Value;

        protected ModelCommand(string undoString) : base(undoString)
        {
        }

        protected ModelCommand(string undoStringSingular, string undoStringPlural,
                               IReadOnlyList<ModelType> models, DataType value)
            : base(undoStringSingular, undoStringPlural, models)
        {
            Value = value;
        }
    }
}
