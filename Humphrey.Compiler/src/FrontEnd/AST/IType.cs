using Humphrey.Backend;

namespace Humphrey.FrontEnd
{
    public interface IType : IAst
    {
        (CompilationType compilationType,IType originalType) CreateOrFetchType(CompilationUnit unit);

        IType ResolveBaseType(SemanticPass pass);
        SemanticPass.IdentifierKind GetBaseType { get; }
        void Semantic(SemanticPass pass);
        bool IsFunctionType { get; }
        AstMetaData MetaData { get; set; }
    }
}
