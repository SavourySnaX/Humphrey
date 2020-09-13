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
            builder.SetDebugLocation(new SourceLocation(Token));

            // Resolve common things
            var codeBlock = initialiser as AstCodeBlock;
            var expr = initialiser as IExpression;
            var exprValue = expr?.ProcessExpression(unit, builder);
            CompilationType ct = null;
            IType ot = default;

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
                (ct,ot) = type.CreateOrFetchType(unit);

            foreach (var ident in identifiers)
            {
                var functionType = ct as CompilationFunctionType;
                if (functionType != null && initialiser == null)
                {
                    // should be scoped
                    unit.CreateNamedType(ident.Dump(), ct, ot);
                }
                else if (functionType != null && initialiser != null && codeBlock!=null)
                {
                    var ft = ot as AstFunctionType;
                    ft.BuildFunction(unit, functionType, ident, codeBlock);
                }
                else if (initialiser == null)
                {
                    // should be scoped
                    unit.CreateNamedType(ident.Dump(), ct, ot);
                }
                else
                {
                    var variableName = ident.Dump();
                    var sourceLocation = new SourceLocation(ident.Token);
                    var newLocal = unit.CreateLocalVariable(unit, builder, ct, variableName, exprValue, sourceLocation);

                    // Debug information
                    var localDbg = unit.CreateAutoVariable(variableName, sourceLocation, ct.DebugType);
                    unit.InsertDeclareAtEnd(newLocal.Storage, localDbg, sourceLocation, builder.CurrentBlock);
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
        private Result<Tokens> _token;
        public Result<Tokens> Token { get => _token; set => _token = value; }

    }
}



