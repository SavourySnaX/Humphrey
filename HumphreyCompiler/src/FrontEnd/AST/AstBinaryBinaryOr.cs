using Humphrey.Backend;

namespace Humphrey.FrontEnd
{
    public class AstBinaryBinaryOr : AstBinaryExpressionExpression
    {
        public AstBinaryBinaryOr(IExpression left, IExpression right) : base(left, right)
        {
        }
    
        public override string DumpOperator()
        {
            return "|";
        }

        public override CompilationConstantIntegerKind CompilationConstantValue(CompilationConstantIntegerKind left, CompilationConstantIntegerKind right)
        {
            left.Or(right);
            return left;
        }

        public override ICompilationValue CompilationValue(CompilationBuilder builder, CompilationValue left, CompilationValue right)
        {
            return builder.Or(left, right);
        }
    }
}



