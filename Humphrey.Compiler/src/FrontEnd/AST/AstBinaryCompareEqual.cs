using Humphrey.Backend;

namespace Humphrey.FrontEnd
{
    public class AstBinaryCompareEqual : AstBinaryExpressionExpression
    {
        public AstBinaryCompareEqual(IExpression left, IExpression right) : base(left,right)
        {
        }

        public override string DumpOperator()
        {
            return "==";
        }

        public override ICompilationConstantValue CompilationConstantValue(ICompilationConstantValue left, ICompilationConstantValue right)
        {
            var l = left as CompilationConstantIntegerKind;
            var r = right as CompilationConstantIntegerKind;
            l.CompareEqual(r);
            return l;
        }

        public override ICompilationValue CompilationValue(CompilationBuilder builder, CompilationValue left, CompilationValue right)
        {
            return builder.Compare(CompilationBuilder.CompareKind.EQ, left, right);
        }
    }
}