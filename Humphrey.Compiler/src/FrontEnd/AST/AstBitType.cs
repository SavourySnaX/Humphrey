using Humphrey.Backend;
namespace Humphrey.FrontEnd
{
    public class AstBitType : IType
    {
        public AstBitType()
        {
        }
    
        public (CompilationType compilationType, IType originalType) CreateOrFetchType(CompilationUnit unit)
        {
            return (unit.FetchIntegerType(1, false, new SourceLocation(Token)), this);
        }

        public bool IsFunctionType => false;
    
        public string Dump()
        {
            return "bit";
        }

        public void Semantic(SemanticPass pass)
        {
            // nothing to do
        }

        public IType ResolveBaseType(SemanticPass pass)
        {
            return this;        // or integerkind(1)??
        }

        private Result<Tokens> _token;
        public Result<Tokens> Token { get => _token; set => _token = value; }

        private AstMetaData metaData;
        public AstMetaData MetaData { get => metaData; set => metaData = value; }

        public SemanticPass.IdentifierKind GetBaseType => SemanticPass.IdentifierKind.Type;
    }
}
