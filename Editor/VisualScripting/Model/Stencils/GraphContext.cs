using System;

namespace UnityEditor.VisualScripting.Model.Stencils
{
    public class GraphContext
    {
        public CSharpTypeSerializer CSharpTypeSerializer { get; }
        public ITypeMetadataResolver TypeMetadataResolver { get; }
        CSharpTypeBasedMetadata.FactoryMethod m_CSharpMetadataFactoryMethod;

        public GraphContext()
        {
            CSharpTypeSerializer = new CSharpTypeSerializer();
            TypeMetadataResolver = CreateMetadataResolver();
        }

        public virtual bool MemberAllowed(MemberInfoValue value) => true;

        ITypeMetadataResolver CreateMetadataResolver()
        {
            return new TypeMetadataResolver(this);
        }

        public bool RequiresInitialization(IVariableDeclarationModel decl)
        {
            if (decl == null)
                return false;

            VariableType variableType = decl.VariableType;
            Type dataType = CSharpTypeSerializer.ResolveType(decl.DataType);

            return (variableType == VariableType.FunctionVariable || variableType == VariableType.GraphVariable) &&
                (dataType.IsValueType || dataType == typeof(string));
        }

        public bool RequiresInspectorInitialization(IVariableDeclarationModel decl)
        {
            Type dataType = CSharpTypeSerializer.ResolveType(decl.DataType);
            return RequiresInitialization(decl);
        }
    }
}
