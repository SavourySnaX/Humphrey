namespace Humphrey.FrontEnd
{
    public class AstDefinition : IExpression, IStatement
    {
        AstIdentifier ident;
        IType type;
        IAssignable initialiser;
        public AstDefinition(AstIdentifier identifier, IType itype, IAssignable init)
        {
            ident = identifier;
            type = itype;
            initialiser = init;
        }
    
        public string Dump()
        {
            if (type==null)
                return $"{ident.Dump()} = {initialiser.Dump()}";
            if (initialiser==null)
                return $"{ident.Dump()} : {type.Dump()}";

            return $"{ident.Dump()} : {type.Dump()} = {initialiser.Dump()}";
        }
    }
}


