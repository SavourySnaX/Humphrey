using Humphrey.Backend;
namespace Humphrey.FrontEnd
{
    public class AstUnaryMinus : IExpression
    {
        IExpression expr;
        public AstUnaryMinus(IExpression expression)
        {
            expr = expression;
        }
    
        public string Dump()
        {
            return $"- {expr.Dump()}";
        }

        public ICompilationConstantValue ProcessConstantExpression(CompilationUnit unit)
        {
            var result = expr.ProcessConstantExpression(unit);
            if (result is CompilationConstantIntegerKind constantValue)
            {
                constantValue.Negate();
                return constantValue;
            }
            else if (result is CompilationConstantFloatKind constantFloat)
            {
                constantFloat.Negate();
                return constantFloat;
            }
            throw new System.NotImplementedException("Unknown constant type");
        }

        public ICompilationValue ProcessExpression(CompilationUnit unit, CompilationBuilder builder)
        {
            var value = expr.ProcessExpression(unit, builder);
            if (value is CompilationConstantIntegerKind constantValue)
            {
                constantValue.Negate();
                return constantValue;
            }
            else if (value is CompilationConstantFloatKind constantFloat)
            {
                constantFloat.Negate();
                return constantFloat;
            }
            else
                return builder.Negate(value as CompilationValue);
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
