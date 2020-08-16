using Humphrey.Backend;

namespace Humphrey.FrontEnd
{
    public class AstBinaryBinaryXor : AstBinaryExpressionExpression
    {
        public AstBinaryBinaryXor(IExpression left, IExpression right) : base(left, right)
        {
        }
    
        public override string DumpOperator()
        {
            return "^";
        }

        public override CompilationConstantValue CompilationConstantValue(CompilationConstantValue left, CompilationConstantValue right)
        {
            left.Xor(right);
            return left;
        }

        public override ICompilationValue CompilationValue(CompilationBuilder builder, CompilationValue left, CompilationValue right)
        {
            return builder.Xor(left, right);
        }
    }
}



