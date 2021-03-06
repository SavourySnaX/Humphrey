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

        public override CompilationConstantIntegerKind CompilationConstantValue(CompilationConstantIntegerKind left, CompilationConstantIntegerKind right)
        {
            left.CompareEqual(right);
            return left;
        }

        public override ICompilationValue CompilationValue(CompilationBuilder builder, CompilationValue left, CompilationValue right)
        {
            return builder.Compare(CompilationBuilder.CompareKind.EQ, left, right);
        }
    }
}