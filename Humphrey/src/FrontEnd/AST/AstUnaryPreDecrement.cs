using Humphrey.Backend;

using static Extensions.Helpers;

namespace Humphrey.FrontEnd
{
    public class AstUnaryPreDecrement : IExpression
    {
        IExpression expr;
        public AstUnaryPreDecrement(IExpression expression)
        {
            expr = expression;
        }
    
        public string Dump()
        {
            return $"-- {expr.Dump()}";
        }

        public CompilationConstantValue ProcessConstantExpression(CompilationUnit unit)
        {
            var result = expr.ProcessConstantExpression(unit);
            result.Decrement();
            return result;
        }

        public ICompilationValue ProcessExpression(CompilationUnit unit, CompilationBuilder builder)
        {
            var value = expr.ProcessExpression(unit, builder);
            if (value is CompilationConstantValue constantValue)
            {
                constantValue.Decrement();
                return constantValue;
            }
            else
            {
                var cv = value as CompilationValue;

                var incByType = cv.Type;
                var incBy = new CompilationValue(incByType.BackendType.CreateConstantValue(1), incByType);
                var incremented=builder.Sub(cv, incBy);
                builder.Store(incremented, cv.Storage);
                return incremented;
            }
        }
        private Result<Tokens> _token;
        public Result<Tokens> Token { get => _token; set => _token = value; }

    }
}
