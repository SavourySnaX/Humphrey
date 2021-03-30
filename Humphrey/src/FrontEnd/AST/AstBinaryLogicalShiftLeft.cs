using Humphrey.Backend;

namespace Humphrey.FrontEnd
{
    public class AstBinaryLogicalShiftLeft : AstBinaryExpressionExpression
    {
        public AstBinaryLogicalShiftLeft(IExpression left, IExpression right) : base(left, right)
        {
        }
    
        public override string DumpOperator()
        {
            return "<<";
        }

        public override CompilationConstantIntegerKind CompilationConstantValue(CompilationConstantIntegerKind left, CompilationConstantIntegerKind right)
        {
            left.LogicalShiftLeft(right);
            return left;
        }

        public override ICompilationValue CompilationValue(CompilationBuilder builder, CompilationValue left, CompilationValue right)
        {
            return builder.LogicalShiftLeft(left, right);
        }
    }
}
