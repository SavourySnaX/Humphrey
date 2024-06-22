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

        public abstract ICompilationConstantValue CompilationConstantValue(ICompilationConstantValue left, ICompilationConstantValue right);

        public ICompilationConstantValue ProcessConstantExpression(CompilationUnit unit)
        {
            var les = lhs.ProcessConstantExpression(unit);
            var res = rhs.ProcessConstantExpression(unit);

            return CompilationConstantValue(les, res);
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

            bool isConstant = rlhs is CompilationConstantIntegerKind || rlhs is CompilationConstantFloatKind;
            isConstant = isConstant && (rrhs is CompilationConstantIntegerKind || rrhs is CompilationConstantFloatKind);
            if (isConstant)
                return ProcessConstantExpression(unit);

            var vlhs = rlhs as CompilationValue;
            var vrhs = rrhs as CompilationValue;

            if (vlhs is null)
                vlhs = (rlhs as CompilationConstantIntegerKind).GetCompilationValue(unit, vrhs.Type);
            if (vrhs is null)
                vrhs = (rrhs as CompilationConstantIntegerKind).GetCompilationValue(unit, vlhs.Type);

            var (valueLeft, valueRight) = AstBinaryExpression.FixupBinaryExpressionInputs(unit, builder, vlhs, vrhs, Token);

            return CompilationValue(builder, valueLeft, valueRight);
        }

        public IType ResolveExpressionType(SemanticPass pass)
        {
            return AstBinaryExpression.ResolveExpressionType(pass, lhs.ResolveExpressionType(pass), rhs.ResolveExpressionType(pass), Token);
        }

        public void Semantic(SemanticPass pass)
        {
            lhs.Semantic(pass);
            rhs.Semantic(pass);
        }

        private Result<Tokens> _token;
        public Result<Tokens> Token { get => _token; set => _token = value; }

    }
}





