using Humphrey.Backend;

namespace Humphrey.FrontEnd
{
    public class AstBinaryMinus : AstBinaryExpressionExpression
    {
        public AstBinaryMinus(IExpression left, IExpression right) : base(left, right)
        {
        }
    
        public override string DumpOperator()
        {
            return "-";
        }

        public override CompilationConstantIntegerKind CompilationConstantValue(CompilationConstantIntegerKind left, CompilationConstantIntegerKind right)
        {
            left.Sub(right);
            return left;
        }

        public override ICompilationValue CompilationValue(CompilationBuilder builder, CompilationValue left, CompilationValue right)
        {
            return builder.Sub(left, right);
        }
    }
}



