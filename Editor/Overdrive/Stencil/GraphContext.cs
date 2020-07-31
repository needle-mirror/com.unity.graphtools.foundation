using System;
using UnityEditor.GraphToolsFoundation.Overdrive.Model;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public class GraphContext
    {
        public ITypeMetadataResolver TypeMetadataResolver { get; }

        public GraphContext()
        {
            TypeMetadataResolver = new TypeMetadataResolver();
        }

        public bool RequiresInitialization(IGTFVariableDeclarationModel decl)
        {
            if (decl == null)
                return false;

            VariableType variableType = decl.VariableType;
            Type dataType = TypeSerializer.ResolveType(decl.DataType);

            return (variableType == VariableType.GraphVariable) && (dataType.IsValueType || dataType == typeof(string));
        }

        public bool RequiresInspectorInitialization(IGTFVariableDeclarationModel decl)
        {
            Type dataType = TypeSerializer.ResolveType(decl.DataType);
            return RequiresInitialization(decl);
        }
    }
}
