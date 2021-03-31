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

        public abstract CompilationConstantIntegerKind CompilationConstantValue(CompilationConstantIntegerKind left, CompilationConstantIntegerKind right);

        public ICompilationConstantValue ProcessConstantExpression(CompilationUnit unit)
        {
            var valueLeft = lhs.ProcessConstantExpression(unit) as CompilationConstantIntegerKind;
            var valueRight = rhs.ProcessConstantExpression(unit) as CompilationConstantIntegerKind;

            return CompilationConstantValue(valueLeft, valueRight);
        }

        public abstract ICompilationValue CompilationValue(CompilationBuilder builder, CompilationValue left, CompilationValue right);
        public ICompilationValue ProcessExpression(CompilationUnit unit, CompilationBuilder builder)
        {
            var rlhs = lhs.ProcessExpression(unit, builder);
            if (rlhs==null)
            {
                throw new CompilationAbortException($"Aborting due to missing symbol");
            }
            var rrhs = rhs.ProcessExpression(unit, builder);
            if (rrhs==null)
            {
                throw new CompilationAbortException($"Aborting due to missing symbol");
            }
            if (rlhs is CompilationConstantIntegerKind clhs && rrhs is CompilationConstantIntegerKind crhs)
                return ProcessConstantExpression(unit);

            var vlhs = rlhs as CompilationValue;
            var vrhs = rrhs as CompilationValue;

            if (vlhs is null)
                vlhs = (rlhs as CompilationConstantIntegerKind).GetCompilationValue(unit, vrhs.Type);
            if (vrhs is null)
                vrhs = (rrhs as CompilationConstantIntegerKind).GetCompilationValue(unit, vlhs.Type);

            var (valueLeft, valueRight) = AstBinaryExpression.FixupBinaryExpressionInputs(unit, builder, vlhs, vrhs);

            return CompilationValue(builder, valueLeft, valueRight);
        }

        private Result<Tokens> _token;
        public Result<Tokens> Token { get => _token; set => _token = value; }

    }
}





