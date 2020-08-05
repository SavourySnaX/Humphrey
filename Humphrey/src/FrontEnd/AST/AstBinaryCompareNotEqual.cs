using Humphrey.Backend;

namespace Humphrey.FrontEnd
{
    public class AstBinaryCompareNotEqual : AstBinaryExpressionExpression
    {
        public AstBinaryCompareNotEqual(IExpression left, IExpression right) : base(left,right)
        {
        }

        public override string DumpOperator()
        {
            return "!=";
        }

        public override CompilationConstantValue CompilationConstantValue(CompilationConstantValue left, CompilationConstantValue right)
        {
            left.CompareNotEqual(right);
            return left;
        }

        public override ICompilationValue CompilationValue(CompilationBuilder builder, CompilationValue left, CompilationValue right)
        {
            return builder.Compare(CompilationBuilder.CompareKind.NE, left, right);
        }
    }
}
