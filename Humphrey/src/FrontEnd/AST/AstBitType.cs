using Humphrey.Backend;
namespace Humphrey.FrontEnd
{
    public class AstBitType : IType
    {
        public AstBitType()
        {
        }
    
        public CompilationType CreateOrFetchType(CompilationUnit unit)
        {
            return unit.FetchIntegerType(1);
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
