using Humphrey.Backend;

namespace Humphrey.FrontEnd
{
    public class AstBinaryLogicalShiftRight : AstBinaryExpressionExpression
    {
        public AstBinaryLogicalShiftRight(IExpression left, IExpression right) : base(left, right)
        {
        }
    
        public override string DumpOperator()
        {
            return ">>";
        }

        public override CompilationConstantIntegerKind CompilationConstantValue(CompilationConstantIntegerKind left, CompilationConstantIntegerKind right)
        {
            left.LogicalShiftRight(right);
            return left;
        }

        public override ICompilationValue CompilationValue(CompilationBuilder builder, CompilationValue left, CompilationValue right)
        {
            return builder.LogicalShiftRight(left, right);
        }
    }
}
