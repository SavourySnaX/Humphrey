using Humphrey.Backend;

namespace Humphrey.FrontEnd
{
    public class AstBinaryBinaryAnd : AstBinaryExpressionExpression
    {
        public AstBinaryBinaryAnd(IExpression left, IExpression right) : base(left, right)
        {
        }
    
        public override string DumpOperator()
        {
            return "&";
        }

        public override CompilationConstantValue CompilationConstantValue(CompilationConstantValue left, CompilationConstantValue right)
        {
            left.And(right);
            return left;
        }

        public override ICompilationValue CompilationValue(CompilationBuilder builder, CompilationValue left, CompilationValue right)
        {
            return builder.And(left, right);
        }
    }
}



