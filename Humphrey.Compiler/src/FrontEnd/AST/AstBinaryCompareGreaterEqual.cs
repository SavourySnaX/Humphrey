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

        public override ICompilationConstantValue CompilationConstantValue(ICompilationConstantValue left, ICompilationConstantValue right)
        {
            var l = left as CompilationConstantIntegerKind;
            var r = right as CompilationConstantIntegerKind;
            l.CompareGreaterEqual(r);
            return l;
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
