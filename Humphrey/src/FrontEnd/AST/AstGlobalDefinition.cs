using System.Text;
using Humphrey.Backend;
namespace Humphrey.FrontEnd
{
    // May need splitting up into AstGlobalDefinition / AstLocalDefinition
    public class AstGlobalDefinition : IExpression, IGlobalDefinition
    {
        AstIdentifier[] identifiers;
        IType type;
        IAssignable initialiser;
        public AstGlobalDefinition(AstIdentifier[] identifierList, IType itype, IAssignable init)
        {
            identifiers = identifierList;
            type = itype;
            initialiser = init;
        }

        public bool Compile(CompilationUnit unit)
        {
            // Resolve common things
            var codeBlock = initialiser as AstCodeBlock;
            var expr = initialiser as IExpression;
            var exprValue = expr?.ProcessConstantExpression(unit);
            CompilationType ct = null;

            if (type == null)
            {
                // Need to compute type from initialiser
                if (expr != null)
                {
                    ct = Expression.ResolveExpressionToValue(unit, exprValue, null).Type;
                }
                else
                {
                    throw new System.Exception($"Type is not computable for functions!");
                }
            }
            else 
                ct = type.CreateOrFetchType(unit);

            foreach (var ident in identifiers)
            {
                var functionType = ct is CompilationFunctionType;
                if (functionType && initialiser==null)
                {
                    unit.CreateNamedType(ident.Dump(), ct);
                }
                else if (functionType && initialiser != null)
                {
                    var newFunction = unit.CreateFunction(ct as CompilationFunctionType, ident.Dump());

                    codeBlock.CreateCodeBlock(unit, newFunction, $"entry_{ident.Dump()}");
                }
                else if (initialiser == null)
                {
                    unit.CreateNamedType(ident.Dump(), ct);
                }
                else
                {
                    // todo this needs to be a constant/computable value for LLVM so we ideally need a semantic pass soon
                    var newGlobal = unit.CreateGlobalVariable(ct, ident.Dump(), exprValue);
                }
            }

            return false;
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


