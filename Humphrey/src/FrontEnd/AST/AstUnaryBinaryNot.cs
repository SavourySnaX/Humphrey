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

        public CompilationConstantValue ProcessConstantExpression(CompilationUnit unit)
        {
            var result = expr.ProcessConstantExpression(unit);
            result.Not();
            return result;
        }

        public ICompilationValue ProcessExpression(CompilationUnit unit, CompilationBuilder builder)
        {
            var value = expr.ProcessExpression(unit, builder);
            if (value is CompilationConstantValue constantValue)
            {
                constantValue.Not();
                return constantValue;
            }
            else
                return builder.Not(value as CompilationValue);
        }
        private Result<Tokens> _token;
        public Result<Tokens> Token { get => _token; set => _token = value; }

    }
}

