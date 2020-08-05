using Humphrey.Backend;

namespace Humphrey.FrontEnd
{
    public class AstBinaryCompareGreaterEqual : AstBinaryExpressionExpression
    {
        public AstBinaryCompareGreaterEqual(IExpression left, IExpression right) : base(left,right)
        {
        }

        public override string DumpOperator()
        {
            return ">=";
        }

        public override CompilationConstantValue CompilationConstantValue(CompilationConstantValue left, CompilationConstantValue right)
        {
            left.CompareGreaterEqual(right);
            return left;
        }

        public override ICompilationValue CompilationValue(CompilationBuilder builder, CompilationValue left, CompilationValue right)
        {
            var leftIntType = left.Type as CompilationIntegerType;
            var rightIntType = right.Type as CompilationIntegerType;

            bool signed = leftIntType.IsSigned || rightIntType.IsSigned;
            return builder.Compare(signed ? CompilationBuilder.CompareKind.SGE : CompilationBuilder.CompareKind.UGE, left, right);
        }
    }
}
