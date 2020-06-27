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
            return unit.CreateFunctionParameter(type.CreateOrFetchType(unit), ident.Dump());
        }

        public bool Compile(CompilationUnit unit)
        {
            return false;
        }
    
        public string Dump()
        {
            return $"{ident.Dump()} : {type.Dump()}";
        }
    }
}

