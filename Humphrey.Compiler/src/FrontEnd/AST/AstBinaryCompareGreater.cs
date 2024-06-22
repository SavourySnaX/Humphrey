using Humphrey.Backend;

namespace Humphrey.FrontEnd
{
    public class AstBinaryCompareGreater : AstBinaryExpressionExpression
    {
        public AstBinaryCompareGreater(IExpression left, IExpression right) : base(left,right)
        {
        }

        public override string DumpOperator()
        {
            return ">";
        }

        public override ICompilationConstantValue CompilationConstantValue(ICompilationConstantValue left, ICompilationConstantValue right)
        {
            var l = left as CompilationConstantIntegerKind;
            var r = right as CompilationConstantIntegerKind;
            l.CompareGreater(r);
            return l;
        }

        public override ICompilationValue CompilationValue(CompilationBuilder builder, CompilationValue left, CompilationValue right)
        {
            var leftIntType = left.Type as CompilationIntegerType;
            var rightIntType = right.Type as CompilationIntegerType;

            bool signed = leftIntType.IsSigned || rightIntType.IsSigned;
            return builder.Compare(signed ? CompilationBuilder.CompareKind.SGT : CompilationBuilder.CompareKind.UGT, left, right);
        }
    }
}
