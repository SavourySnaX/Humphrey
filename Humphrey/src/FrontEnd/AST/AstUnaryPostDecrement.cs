using Humphrey.Backend;

using static Extensions.Helpers;

namespace Humphrey.FrontEnd
{
    public class AstUnaryPostDecrement : IExpression
    {
        IExpression expr;
        public AstUnaryPostDecrement(IExpression expression)
        {
            expr = expression;
        }
    
        public string Dump()
        {
            return $"{expr.Dump()} --";
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

                var decByType = cv.Type;
                var decBy = new CompilationValue(decByType.BackendType.CreateConstantValue(1), decByType);
                var incremented=builder.Sub(cv, decBy);
                builder.Store(incremented, cv.Storage);
                return cv;
            }
        }
        private Result<Tokens> _token;
        public Result<Tokens> Token { get => _token; set => _token = value; }

    }
}
