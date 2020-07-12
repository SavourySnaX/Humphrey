using Humphrey.Backend;
namespace Humphrey.FrontEnd
{
    public class AstStructElement : IExpression
    {
        AstIdentifier ident;
        IType type;
        IAssignable initialiser;
        public AstStructElement(AstIdentifier identifier, IType itype, IAssignable init)
        {
            ident = identifier;
            type = itype;
            initialiser = init;
        }

        public bool Compile(CompilationUnit unit)
        {
            var ct = type.CreateOrFetchType(unit);

            if (type.IsFunctionType)
            {
                throw new System.NotSupportedException($"functions are not members of structs");
            }
            else if (initialiser==null)
            {
                throw new System.NotImplementedException($"TODO elementType type");
            }
            else
            {
                throw new System.NotImplementedException($"TODO structure value on allocation");
            }
        }
    
        public string Dump()
        {
            if (type==null)
                return $"{ident.Dump()} = {initialiser.Dump()}";
            if (initialiser==null)
                return $"{ident.Dump()} : {type.Dump()}";

            return $"{ident.Dump()} : {type.Dump()} = {initialiser.Dump()}";
        }

        public IType Type => type;

        public CompilationConstantValue ProcessConstantExpression(CompilationUnit unit)
        {
            throw new System.NotImplementedException($"Todo implement constant expression processing");
        }

        public ICompilationValue ProcessExpression(CompilationUnit unit, CompilationBuilder builder)
        {
            throw new System.NotImplementedException();
        }
    }
}



