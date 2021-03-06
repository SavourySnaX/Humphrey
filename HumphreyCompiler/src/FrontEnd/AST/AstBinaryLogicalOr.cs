using Humphrey.Backend;

namespace Humphrey.FrontEnd
{
    public class AstBinaryLogicalOr : AstBinaryExpressionExpression
    {
        public AstBinaryLogicalOr(IExpression left, IExpression right) : base(left, right)
        {
        }
    
        public override string DumpOperator()
        {
            return "||";
        }

        public override CompilationConstantIntegerKind CompilationConstantValue(CompilationConstantIntegerKind left, CompilationConstantIntegerKind right)
        {
            left.LogicalOr(right);
            return left;
        }

        public override ICompilationValue CompilationValue(CompilationBuilder builder, CompilationValue left, CompilationValue right)
        {
            return builder.LogicalOr(left, right);
        }
    }
}



