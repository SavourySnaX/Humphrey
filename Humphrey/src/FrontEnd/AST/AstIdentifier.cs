using Humphrey.Backend;

namespace Humphrey.FrontEnd
{
    public class AstIdentifier : IExpression, IType, IIdentifier
    {
        string name;
        public AstIdentifier(string value)
        {
            name = value;
        }
    
        public (CompilationType compilationType, IType originalType) CreateOrFetchType(CompilationUnit unit)
        {
            return unit.FetchNamedType(this);
        }
    
        public bool IsFunctionType => false;
    
        public string Dump()
        {
            return name;
        }
        public CompilationConstantValue ProcessConstantExpression(CompilationUnit unit)
        {
            throw new System.NotImplementedException($"Todo implement constant expression processing for constant values");
        }

        public ICompilationValue ProcessExpression(CompilationUnit unit, CompilationBuilder builder)
        {
            throw new System.NotImplementedException($"Todo implement expression processing for non loadable identifier");
        }
        public string Name => name;
        private Result<Tokens> _token;
        public Result<Tokens> Token { get => _token; set => _token = value; }

        private AstMetaData metaData;
        public AstMetaData MetaData { get => metaData; set => metaData = value; }
    }
}
