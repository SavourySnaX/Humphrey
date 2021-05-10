using Humphrey.Backend;
namespace Humphrey.FrontEnd
{
    public class AstGenericType : IType
    {
        private IType belongsTo;
        CommonSymbolTable symbolTable;

        public AstGenericType()
        {
            belongsTo=null;
        }

        public void SetInstanceType(IType type, CommonSymbolTable currentScope)
        {
            belongsTo = type;
            symbolTable=currentScope;
        }

        public (CompilationType compilationType, IType originalType) CreateOrFetchType(CompilationUnit unit)
        {
            // TODO LINK THE TYPE BACK TO THE FUNCTION PARAMETER, so we can actually resolve ourselves
            var oldScope = unit.PushScope(symbolTable, unit.DebugScope);
            var result = belongsTo.CreateOrFetchType(unit);
            unit.PopScope(oldScope);
            return result;
        }

        public string Dump()
        {
            return "_";
        }

        public IType ResolveBaseType(SemanticPass pass)
        {
            return this;
        }

        public void Semantic(SemanticPass pass)
        {
        }

        public bool IsFunctionType => false;
        private Result<Tokens> _token;
        public Result<Tokens> Token { get => _token; set => _token = value; }

        private AstMetaData metaData;
        public AstMetaData MetaData { get => metaData; set => metaData = value; }

        public SemanticPass.IdentifierKind GetBaseType => SemanticPass.IdentifierKind.FunctionParam;

    }
}