using Humphrey.Backend;

namespace Humphrey.FrontEnd
{
    public class AstBinaryLessThan : IExpression
    {
        IExpression lhs;
        IExpression rhs;
        public AstBinaryLessThan(IExpression left, IExpression right)
        {
            lhs = left;
            rhs = right;
        }
    
        public string Dump()
        {
            return $"< {lhs.Dump()} {rhs.Dump()}";
        }

        public CompilationConstantValue ProcessConstantExpression(CompilationUnit unit)
        {
            var valueLeft = lhs.ProcessConstantExpression(unit);
            var valueRight = rhs.ProcessConstantExpression(unit);

            valueLeft.LessThan(valueRight);

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

            bool signed = false;
            if (valueLeft.Type is CompilationIntegerType vlt)
            {
                if (vlt.IsSigned)
                    signed = true;
            }

            return builder.Compare(signed ? CompilationBuilder.CompareKind.SLT : CompilationBuilder.CompareKind.ULT, valueLeft, valueRight);
        }
    }
}



