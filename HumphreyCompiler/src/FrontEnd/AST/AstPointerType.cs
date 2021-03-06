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
            if (ct==null)
                throw new CompilationAbortException($"Attempt to create a pointer to an unknown type {elementType.Dump()}");
            return (unit.CreatePointerType(ct, new SourceLocation(Token)), this);
        }
    
        public bool IsFunctionType => false;

        public string Dump()
        {
            return $"* {elementType.Dump()}";
        }

        public void Semantic(SemanticPass pass)
        {
            elementType.Semantic(pass);
        }

        public IType ResolveBaseType(SemanticPass pass)
        {
            return this;
        }

        public IType ElementType => elementType;
        private Result<Tokens> _token;
        public Result<Tokens> Token { get => _token; set => _token = value; }
        private AstMetaData metaData;
        public AstMetaData MetaData { get => metaData; set => metaData = value; }

        public SemanticPass.IdentifierKind GetBaseType => elementType.GetBaseType;
    }
}



