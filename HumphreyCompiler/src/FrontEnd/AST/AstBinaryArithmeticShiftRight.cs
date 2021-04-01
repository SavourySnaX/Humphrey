using Humphrey.Backend;

namespace Humphrey.FrontEnd
{
    public class AstBinaryArithmeticShiftRight : AstBinaryExpressionExpression
    {
        public AstBinaryArithmeticShiftRight(IExpression left, IExpression right) : base(left, right)
        {
        }
    
        public override string DumpOperator()
        {
            return ">>>";
        }

        public override CompilationConstantIntegerKind CompilationConstantValue(CompilationConstantIntegerKind left, CompilationConstantIntegerKind right)
        {
            left.ArithmeticShiftRight(right);
            return left;
        }

        public override ICompilationValue CompilationValue(CompilationBuilder builder, CompilationValue left, CompilationValue right)
        {
            return builder.ArithmeticShiftRight(left, right);
        }
    }
}

