using Humphrey.Backend;
namespace Humphrey.FrontEnd
{
    // May need splitting up into AstGlobalDefinition / AstLocalDefinition
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

        public bool BuildStatement(CompilationUnit unit, CompilationFunction function, CompilationBuilder builder)
        {
            throw new System.NotImplementedException();
        }

        public bool Compile(CompilationUnit unit)
        {
            var ct = type.CreateOrFetchType(unit);


            if (type.IsFunctionType && initialiser==null)
            {
                // functionptr as global variable
                //unit.CreateGlobalVariable(type, ident.Dump());
                throw new System.NotImplementedException($"TODO functionptr type");
            }
            else if (type.IsFunctionType && initialiser!=null)
            {
                // function

                // Todo output parameters need to be marked nonnull dereferencable at the least
                var newFunction = unit.CreateFunction(ct as CompilationFunctionType, ident.Dump());

                var codeBlock = initialiser as AstCodeBlock;

                codeBlock.CreateCodeBlock(unit, newFunction);
            }
            else
            {
                // variable with/without initialiser
                throw new System.NotImplementedException($"TODO global variable");
            }            
            

            return false;
        }
    
        public string Dump()
        {
            if (type==null)
                return $"{ident.Dump()} = {initialiser.Dump()}";
            if (initialiser==null)
                return $"{ident.Dump()} : {type.Dump()}";

            return $"{ident.Dump()} : {type.Dump()} = {initialiser.Dump()}";
        }

        public CompilationValue ProcessExpression(CompilationUnit unit, CompilationBuilder builder)
        {
            throw new System.NotImplementedException();
        }
    }
}


