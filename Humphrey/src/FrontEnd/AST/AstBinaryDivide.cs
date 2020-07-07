using Humphrey.Backend;

namespace Humphrey.FrontEnd
{
    public class AstBinaryDivide : IExpression
    {
        IExpression lhs;
        IExpression rhs;
        public AstBinaryDivide(IExpression left, IExpression right)
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
            return $"/ {lhs.Dump()} {rhs.Dump()}";
        }

        public CompilationConstantValue ProcessConstantExpression(CompilationUnit unit)
        {
            var valueLeft = lhs.ProcessConstantExpression(unit);
            var valueRight = rhs.ProcessConstantExpression(unit);

            valueLeft.Div(valueRight);

            return valueLeft;
        }

        public CompilationValue ProcessExpression(CompilationUnit unit, CompilationBuilder builder)
        {
            var (valueLeft, valueRight) = AstBinaryExpression.FixupBinaryExpressionInputs(unit, builder, lhs, rhs);

            if (valueLeft.Type.IsSigned)
                return builder.SDiv(valueLeft, valueRight);

            return builder.UDiv(valueLeft, valueRight);
        }
    }
}



