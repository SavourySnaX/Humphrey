using Humphrey.Backend;
namespace Humphrey.FrontEnd
{
    public class AstParamDefinition : IAst
    {
        AstIdentifier ident;
        IType type;
        public AstParamDefinition(AstIdentifier identifier, IType itype)
        {
            ident = identifier;
            type = itype;
        }
    
        public CompilationParam FetchParam(CompilationUnit unit)
        {
            return unit.CreateFunctionParameter(type.CreateOrFetchType(unit).compilationType, ident);
        }
        public CompilationParam FetchParam(CompilationUnit unit, IType inputType)
        {
            (type as AstGenericType).SetInstanceType(inputType);
            
            return unit.CreateFunctionParameter(inputType.CreateOrFetchType(unit).compilationType, ident);
        }


        public string Dump()
        {
            return $"{ident.Dump()} : {type.Dump()}";
        }

        public bool IsGeneric => type is AstGenericType;

        public IType Type => type;
        public AstIdentifier Identifier => ident;

        private Result<Tokens> _token;
        public Result<Tokens> Token { get => _token; set => _token = value; }

    }
}

