using Humphrey.Backend;
namespace Humphrey.FrontEnd
{
    public class AstUnaryLogicalNot : IExpression
    {
        IExpression expr;
        public AstUnaryLogicalNot(IExpression expression)
        {
            expr = expression;
        }
    
        public string Dump()
        {
            return $"! {expr.Dump()}";
        }

        public CompilationConstantValue ProcessConstantExpression(CompilationUnit unit)
        {
            throw new System.NotImplementedException($"TODO - Logical Not on constant expression???");
            var result = expr.ProcessConstantExpression(unit);
            result.Negate();
            return result;
        }

        public ICompilationValue ProcessExpression(CompilationUnit unit, CompilationBuilder builder)
        {
            throw new System.NotImplementedException($"TODO - Logical Not on expression???");
            var value = expr.ProcessExpression(unit, builder);
            if (value is CompilationConstantValue constantValue)
            {
                constantValue.Negate();
                return constantValue;
            }
            else
                return builder.Negate(value as CompilationValue);
        }
        private Result<Tokens> _token;
        public Result<Tokens> Token { get => _token; set => _token = value; }

    }
}

