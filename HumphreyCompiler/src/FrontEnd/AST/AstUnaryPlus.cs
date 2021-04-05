using Humphrey.Backend;
namespace Humphrey.FrontEnd
{
    public class AstUnaryPlus : IExpression
    {
        IExpression expr;
        public AstUnaryPlus(IExpression expression)
        {
            expr = expression;
        }
    
        public string Dump()
        {
            return $"+ {expr.Dump()}";
        }

        public ICompilationConstantValue ProcessConstantExpression(CompilationUnit unit)
        {
            // + is 0 + which is noop
            return expr.ProcessConstantExpression(unit);
        }

        public ICompilationValue ProcessExpression(CompilationUnit unit, CompilationBuilder builder)
        {
            return expr.ProcessExpression(unit, builder);
        }

        public IType ResolveExpressionType(SemanticPass pass)
        {
            return expr.ResolveExpressionType(pass);
        }

        public void Semantic(SemanticPass pass)
        {
            expr.Semantic(pass);
        }

        private Result<Tokens> _token;
        public Result<Tokens> Token { get => _token; set => _token = value; }

    }
}

