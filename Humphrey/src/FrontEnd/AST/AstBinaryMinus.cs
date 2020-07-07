using Humphrey.Backend;

namespace Humphrey.FrontEnd
{
    public class AstBinaryMinus : IExpression
    {
        IExpression lhs;
        IExpression rhs;
        public AstBinaryMinus(IExpression left, IExpression right)
        {
            lhs = left;
            rhs = right;
        }
    
        public bool Compile(CompilationUnit unit)
        {
            return false;
        }

        public string Dump()
        {
            return $"- {lhs.Dump()} {rhs.Dump()}";
        }

        public CompilationConstantValue ProcessConstantExpression(CompilationUnit unit)
        {
            var valueLeft = lhs.ProcessConstantExpression(unit);
            var valueRight = rhs.ProcessConstantExpression(unit);

            valueLeft.Sub(valueRight);

            return valueLeft;
        }

        public CompilationValue ProcessExpression(CompilationUnit unit, CompilationBuilder builder)
        {
            var (valueLeft, valueRight) = AstBinaryExpression.FixupBinaryExpressionInputs(unit, builder, lhs, rhs);

            return builder.Sub(valueLeft, valueRight);
        }
    }
}



