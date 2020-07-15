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
    }
}
