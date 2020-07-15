using System.Text;
using Humphrey.Backend;
namespace Humphrey.FrontEnd
{
    // May need splitting up into AstGlobalDefinition / AstLocalDefinition
    public class AstLocalDefinition : IExpression, IStatement
    {
        AstIdentifier[] identifiers;
        IType type;
        IAssignable initialiser;
        public AstLocalDefinition(AstIdentifier[] identifierList, IType itype, IAssignable init)
        {
            identifiers = identifierList;
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
            /*
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

            return false;*/
        }
    
        public string Dump()
        {
            var s = new StringBuilder();
            for (int a=0;a<identifiers.Length;a++)
            {
                if (a!=0)
                    s.Append(" , ");
                s.Append(identifiers[a].Dump());
            }
            if (type==null)
                s.Append($" := {initialiser.Dump()}");
            else if (initialiser==null)
                s.Append($" : {type.Dump()}");
            else
                s.Append($" : {type.Dump()} = {initialiser.Dump()}");

            return s.ToString();
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



