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
    
        public bool Compile(CompilationUnit unit)
        {
            return false;
        }
    
        public string Dump()
        {
            return $"- {expr.Dump()}";
        }

        public CompilationConstantValue ProcessConstantExpression(CompilationUnit unit)
        {
            var result = expr.ProcessConstantExpression(unit);
            result.Negate();
            return result;
        }

        public CompilationValue ProcessExpression(CompilationUnit unit, CompilationBuilder builder)
        {
            var value = expr.ProcessExpression(unit, builder);

            var result = builder.BackendValue.BuildSub(unit.CreateConstant("0").BackendValue, value.BackendValue);

            return new CompilationValue(result);
        }
    }
}
