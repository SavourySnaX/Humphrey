using Humphrey.Backend;
namespace Humphrey.FrontEnd
{
    public class AstUnaryBinaryNot : IExpression
    {
        IExpression expr;
        public AstUnaryBinaryNot(IExpression expression)
        {
            expr = expression;
        }
    
        public string Dump()
        {
            return $"~ {expr.Dump()}";
        }

        public ICompilationConstantValue ProcessConstantExpression(CompilationUnit unit)
        {
            var result = expr.ProcessConstantExpression(unit) as CompilationConstantIntegerKind;
            result.Not();
            return result;
        }

        public ICompilationValue ProcessExpression(CompilationUnit unit, CompilationBuilder builder)
        {
            var value = expr.ProcessExpression(unit, builder);
            if (value is CompilationConstantIntegerKind constantValue)
            {
                constantValue.Not();
                return constantValue;
            }
            else
                return builder.Not(value as CompilationValue);
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

