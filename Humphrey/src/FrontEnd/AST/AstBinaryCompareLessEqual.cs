using Humphrey.Backend;

namespace Humphrey.FrontEnd
{
    public class AstBinaryCompareLessEqual : AstBinaryExpressionExpression
    {
        public AstBinaryCompareLessEqual(IExpression left, IExpression right) : base(left,right)
        {
        }

        public override string DumpOperator()
        {
            return "<=";
        }

        public override CompilationConstantIntegerKind CompilationConstantValue(CompilationConstantIntegerKind left, CompilationConstantIntegerKind right)
        {
            left.CompareLessEqual(right);
            return left;
        }

        public override ICompilationValue CompilationValue(CompilationBuilder builder, CompilationValue left, CompilationValue right)
        {
            var leftIntType = left.Type as CompilationIntegerType;
            var rightIntType = right.Type as CompilationIntegerType;

            bool signed = leftIntType.IsSigned || rightIntType.IsSigned;
            return builder.Compare(signed ? CompilationBuilder.CompareKind.SLE : CompilationBuilder.CompareKind.ULE, left, right);
        }
    }
}
