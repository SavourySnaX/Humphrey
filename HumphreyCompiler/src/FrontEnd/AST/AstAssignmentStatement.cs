using Humphrey.Backend;
namespace Humphrey.FrontEnd
{
    public class AstAssignmentStatement : IStatement
    {
        AstExpressionList exprList;
        IAssignable assignable;
        public AstAssignmentStatement(AstExpressionList expressionList, IAssignable assign)
        {
            exprList = expressionList;
            assignable = assign;
        }

        public bool BuildStatement(CompilationUnit unit, CompilationFunction function, CompilationBuilder builder)
        {
            builder.SetDebugLocation(new SourceLocation(Token));

            // Resolve common things
            var expr = assignable as IExpression;

            if (expr == null)
                throw new System.NotImplementedException($"CodeBlock assignment not supported");

            foreach (var dest in exprList.Expressions)
            {
                var store = dest as IStorable;

                store.ProcessExpressionForStore(unit, builder, expr);
            }

            return true;
        }

        public string Dump()
        {
            return $"{exprList.Dump()} = {assignable.Dump()}";
        }

        public void Semantic(SemanticPass pass)
        {
            var expr = assignable as IExpression;
            if (expr == null)
            {
                pass.Messages.Log(CompilerErrorKind.Error_MustBeExpression, "Right hand side of assignment must be an expression", Token.Location, Token.Remainder);
            }
            else
            {
                expr.ResolveExpressionType(pass);
                expr.Semantic(pass);
            }
            foreach (var dest in exprList.Expressions)
            {
                dest.ResolveExpressionType(pass);
                var store = dest as IStorable;
                store.Semantic(pass);
            }
        }

        private Result<Tokens> _token;
        public Result<Tokens> Token { get => _token; set => _token = value; }

    }
}


