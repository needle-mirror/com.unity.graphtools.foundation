using System;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public class GraphContext
    {
        public ITypeMetadataResolver TypeMetadataResolver { get; }

        public GraphContext()
        {
            TypeMetadataResolver = new TypeMetadataResolver();
        }

        public bool RequiresInitialization(IVariableDeclarationModel decl)
        {
            if (decl == null)
                return false;

            Type dataType = TypeSerializer.ResolveType(decl.DataType);

            return dataType.IsValueType || dataType == typeof(string);
        }

        public bool RequiresInspectorInitialization(IVariableDeclarationModel decl)
        {
            return RequiresInitialization(decl);
        }
    }
}
