using Humphrey.Backend;

namespace Humphrey.FrontEnd
{
    public class AstBinaryCompareEqual : IExpression
    {
        IExpression lhs;
        IExpression rhs;
        public AstBinaryCompareEqual(IExpression left, IExpression right)
        {
            lhs = left;
            rhs = right;
        }
    
        public string Dump()
        {
            return $"== {lhs.Dump()} {rhs.Dump()}";
        }

        public CompilationConstantValue ProcessConstantExpression(CompilationUnit unit)
        {
            throw new System.NotImplementedException($"TODO - ConstantExpression EqualsEquals");
            var valueLeft = lhs.ProcessConstantExpression(unit);
            var valueRight = rhs.ProcessConstantExpression(unit);

            valueLeft.Div(valueRight);

            return valueLeft;
        }

        public ICompilationValue ProcessExpression(CompilationUnit unit, CompilationBuilder builder)
        {
            throw new System.NotImplementedException($"TODO - Expression EqualsEquals");
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

            var leftIntType = valueLeft.Type as CompilationIntegerType;
            var rightIntType = valueRight.Type as CompilationIntegerType;

            if (leftIntType.IsSigned || rightIntType.IsSigned)
                return builder.SDiv(valueLeft, valueRight);

            return builder.UDiv(valueLeft, valueRight);
        }
        private Result<Tokens> _token;
        public Result<Tokens> Token { get => _token; set => _token = value; }

    }
}




