using Humphrey.Backend;

namespace Humphrey.FrontEnd
{
    public class AstIdentifier : IExpression, IType, IIdentifier
    {
        string name;
        private bool semanticDone;
        public AstIdentifier(string value)
        {
            name = value;
            semanticDone = false;
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
        public ICompilationConstantValue ProcessConstantExpression(CompilationUnit unit)
        {
            throw new System.NotImplementedException($"Todo implement constant expression processing for constant values");
        }

        public ICompilationValue ProcessExpression(CompilationUnit unit, CompilationBuilder builder)
        {
            throw new System.NotImplementedException($"Todo implement expression processing for non loadable identifier");
        }

        public IType ResolveExpressionType(SemanticPass pass)
        {
            throw new System.NotImplementedException();
        }

        public void Semantic(SemanticPass pass)
        {
            if (!semanticDone)
            {
                semanticDone = true;
                if (!pass.AddSemanticLocation(this, Token))
                {
                    pass.Messages.Log(CompilerErrorKind.Error_UndefinedType, $"Type '{Name}' is not found in the current scope.", Token.Location, Token.Remainder);
                }
            }
        }

        public IType ResolveBaseType(SemanticPass pass)
        {
            return pass.FetchNamedType(this);
        }

        public string Name => name;
        private Result<Tokens> _token;
        public Result<Tokens> Token { get => _token; set => _token = value; }

        private AstMetaData metaData;
        public AstMetaData MetaData { get => metaData; set => metaData = value; }

        public SemanticPass.IdentifierKind GetBaseType => SemanticPass.IdentifierKind.None;
    }
}
