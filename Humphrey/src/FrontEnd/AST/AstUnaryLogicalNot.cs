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

        public ICompilationConstantValue ProcessConstantExpression(CompilationUnit unit)
        {
            var result = expr.ProcessConstantExpression(unit) as CompilationConstantIntegerKind;
            result.LogicalNot();
            return result;
        }

        public ICompilationValue ProcessExpression(CompilationUnit unit, CompilationBuilder builder)
        {
            var value = expr.ProcessExpression(unit, builder);
            if (value is CompilationConstantIntegerKind constantValue)
            {
                constantValue.LogicalNot();
                return constantValue;
            }
            else
                return builder.LogicalNot(value as CompilationValue);
        }
        private Result<Tokens> _token;
        public Result<Tokens> Token { get => _token; set => _token = value; }

    }
}

