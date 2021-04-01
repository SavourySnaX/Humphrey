using Humphrey.Backend;

namespace Humphrey.FrontEnd
{
    public class AstBinaryMultiply : AstBinaryExpressionExpression
    {
        public AstBinaryMultiply(IExpression left, IExpression right) : base(left, right)
        {
        }
    
        public override string DumpOperator()
        {
            return "*";
        }

        public override CompilationConstantIntegerKind CompilationConstantValue(CompilationConstantIntegerKind left, CompilationConstantIntegerKind right)
        {
            left.Mul(right);
            return left;
        }

        public override ICompilationValue CompilationValue(CompilationBuilder builder, CompilationValue left, CompilationValue right)
        {
            return builder.Mul(left, right);
        }
    }
}



