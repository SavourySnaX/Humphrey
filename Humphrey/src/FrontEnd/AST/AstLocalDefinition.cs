using Humphrey.Backend;
namespace Humphrey.FrontEnd
{
    // May need splitting up into AstGlobalDefinition / AstLocalDefinition
    public class AstLocalDefinition : IExpression, IStatement
    {
        AstIdentifier ident;
        IType type;
        IAssignable initialiser;
        public AstLocalDefinition(AstIdentifier identifier, IType itype, IAssignable init)
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

            throw new System.NotSupportedException($"local definitions are not supported yet");
            
            if (ct.IsFunctionType && initialiser==null)
            {
                unit.CreateNamedType(ident.Dump(), ct);
            }
            else if (ct.IsFunctionType && initialiser!=null)
            {
                var newFunction = unit.CreateFunction(ct as CompilationFunctionType, ident.Dump());

                var codeBlock = initialiser as AstCodeBlock;

                codeBlock.CreateCodeBlock(unit, newFunction);
            }
            else if (initialiser==null)
            {
                unit.CreateNamedType(ident.Dump(), ct);
            }
            else
            {
                var expr = initialiser as IExpression;

                var exprValue = expr.ProcessConstantExpression(unit);

                var newGlobal = unit.CreateGlobalVariable(ct, ident.Dump(), exprValue);
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



