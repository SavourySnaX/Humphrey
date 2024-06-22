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

        public override ICompilationConstantValue CompilationConstantValue(ICompilationConstantValue left, ICompilationConstantValue right)
        {
            var l = left as CompilationConstantIntegerKind;
            var r = right as CompilationConstantIntegerKind;
            l.Xor(r);
            return l;
        }

        public override ICompilationValue CompilationValue(CompilationBuilder builder, CompilationValue left, CompilationValue right)
        {
            return builder.Xor(left, right);
        }
    }
}



