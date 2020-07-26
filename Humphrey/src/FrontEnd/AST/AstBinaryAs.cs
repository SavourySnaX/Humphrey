using Humphrey.Backend;

namespace Humphrey.FrontEnd
{
    public class AstBinaryAs : IExpression
    {
        IExpression lhs;
        IType rhs;
        public AstBinaryAs(IExpression left, IType right)
        {
            lhs = left;
            rhs = right;
        }
    
        public string Dump()
        {
            return $"as {lhs.Dump()} {rhs.Dump()}";
        }

        public CompilationConstantValue ProcessConstantExpression(CompilationUnit unit)
        {
            var valueLeft = lhs.ProcessConstantExpression(unit);
            valueLeft.Cast(rhs);
            return valueLeft;
        }

        public ICompilationValue ProcessExpression(CompilationUnit unit, CompilationBuilder builder)
        {
            var vlhs = lhs.ProcessExpression(unit, builder);
            if (vlhs is CompilationConstantValue)
                return ProcessConstantExpression(unit);

            throw new System.NotImplementedException($"Todo ProcessExpression as");
            /*
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

            if (valueLeft.Type.IsSigned || valueRight.Type.IsSigned)
                return builder.SDiv(valueLeft, valueRight);

            return builder.UDiv(valueLeft, valueRight);
            */
        }
    }
}




