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

        public ICompilationValue ProcessExpression(CompilationUnit unit, CompilationBuilder builder)
        {
            var rlhs = lhs.ProcessExpression(unit, builder);
            var rrhs = rhs.ProcessExpression(unit, builder);
            if (rlhs is CompilationConstantValue clhs && rrhs is CompilationConstantValue crhs)
                return ProcessConstantExpression(unit);

            var vlhs = rlhs as CompilationValue;
            var vrhs = rrhs as CompilationValue;

            if (vlhs is null)
                vlhs = (rlhs as CompilationConstantValue).GetCompilationValue(unit, vrhs.Type);
            if (vrhs is null)
                vrhs = (rrhs as CompilationConstantValue).GetCompilationValue(unit, vlhs.Type);

            var (valueLeft, valueRight) = AstBinaryExpression.FixupBinaryExpressionInputs(unit, builder, vlhs, vrhs);

            return builder.Sub(valueLeft, valueRight);
        }
    }
}


