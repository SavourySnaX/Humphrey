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
        private Result<Tokens> _token;
        public Result<Tokens> Token { get => _token; set => _token = value; }

    }
}
