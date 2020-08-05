using Humphrey.Backend;

namespace Humphrey.FrontEnd
{
    public abstract class AstBinaryExpressionExpression : IExpression
    {
        IExpression lhs;
        IExpression rhs;
        public AstBinaryExpressionExpression(IExpression left, IExpression right)
        {
            lhs = left;
            rhs = right;
        }

        public abstract string DumpOperator();

        public string Dump()
        {
            return $"{DumpOperator()} {lhs.Dump()} {rhs.Dump()}";
        }

        public abstract CompilationConstantValue CompilationConstantValue(CompilationConstantValue left, CompilationConstantValue right);

        public CompilationConstantValue ProcessConstantExpression(CompilationUnit unit)
        {
            var valueLeft = lhs.ProcessConstantExpression(unit);
            var valueRight = rhs.ProcessConstantExpression(unit);

            return CompilationConstantValue(valueLeft, valueRight);
        }

        public abstract ICompilationValue CompilationValue(CompilationBuilder builder, CompilationValue left, CompilationValue right);
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

            return CompilationValue(builder, valueLeft, valueRight);
        }

        private Result<Tokens> _token;
        public Result<Tokens> Token { get => _token; set => _token = value; }

    }
}





