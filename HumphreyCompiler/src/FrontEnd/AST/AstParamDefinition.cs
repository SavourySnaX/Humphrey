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

        public string Dump()
        {
            return $"{ident.Dump()} : {type.Dump()}";
        }
        private Result<Tokens> _token;
        public Result<Tokens> Token { get => _token; set => _token = value; }

    }
}

