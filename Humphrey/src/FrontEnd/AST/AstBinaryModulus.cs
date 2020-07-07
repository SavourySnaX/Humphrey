using Humphrey.Backend;

namespace Humphrey.FrontEnd
{
    public class AstBinaryModulus : IExpression
    {
        IExpression lhs;
        IExpression rhs;
        public AstBinaryModulus(IExpression left, IExpression right)
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
            return $"% {lhs.Dump()} {rhs.Dump()}";
        }

        public CompilationConstantValue ProcessConstantExpression(CompilationUnit unit)
        {
            var valueLeft = lhs.ProcessConstantExpression(unit);
            var valueRight = rhs.ProcessConstantExpression(unit);

            valueLeft.Rem(valueRight);

            return valueLeft;
        }

        public CompilationValue ProcessExpression(CompilationUnit unit, CompilationBuilder builder)
        {
            var (valueLeft, valueRight) = AstBinaryExpression.FixupBinaryExpressionInputs(unit, builder, lhs, rhs);

            if (valueLeft.Type.IsSigned)
                return builder.SRem(valueLeft, valueRight);

            return builder.URem(valueLeft, valueRight);
        }
    }
}




