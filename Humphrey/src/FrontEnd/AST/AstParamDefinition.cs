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
    
        public string Dump()
        {
            return $"{ident.Dump()} : {type.Dump()}";
        }
    }
}

