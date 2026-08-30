using Humphrey.Backend;
namespace Humphrey.FrontEnd
{
    public class AstFp64Type : IType
    {
        public AstFp64Type()
        {
        }
    
        public (CompilationType compilationType, IType originalType) CreateOrFetchType(CompilationUnit unit)
        {
            return (unit.FetchDoubleType(new SourceLocation(Token)), this);
        }

        public bool IsFunctionType => false;
    
        public string Dump()
        {
            return "fp64";
        }

        public void Semantic(SemanticPass pass)
        {
            // nothing to do
        }

        public IType ResolveBaseType(SemanticPass pass)
        {
            return this;
        }

        private Result<Tokens> _token;
        public Result<Tokens> Token { get => _token; set => _token = value; }

        private AstMetaData metaData;
        public AstMetaData MetaData { get => metaData; set => metaData = value; }

        public SemanticPass.IdentifierKind GetBaseType => SemanticPass.IdentifierKind.Type;
    }
}
