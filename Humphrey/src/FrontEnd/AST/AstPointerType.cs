using static Extensions.Helpers;
using Humphrey.Backend;
namespace Humphrey.FrontEnd
{
    public class AstPointerType : IType
    {
        IType elementType;
        public AstPointerType(IType element)
        {
            elementType = element;
        }
    
        public (CompilationType compilationType, IType originalType) CreateOrFetchType(CompilationUnit unit)
        {
            var (ct,ot) = elementType.CreateOrFetchType(unit);
            return (unit.CreatePointerType(ct, new SourceLocation(Token)), this);
        }
    
        public bool IsFunctionType => false;

        public string Dump()
        {
            return $"* {elementType.Dump()}";
        }

        public IType ElementType => elementType;
        private Result<Tokens> _token;
        public Result<Tokens> Token { get => _token; set => _token = value; }

    }
}



