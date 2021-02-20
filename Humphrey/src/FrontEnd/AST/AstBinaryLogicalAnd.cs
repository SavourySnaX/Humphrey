using Humphrey.Backend;

namespace Humphrey.FrontEnd
{
    public class AstBinaryLogicalAnd : AstBinaryExpressionExpression
    {
        public AstBinaryLogicalAnd(IExpression left, IExpression right) : base(left, right)
        {
        }
    
        public override string DumpOperator()
        {
            return "&&";
        }

        public override CompilationConstantIntegerKind CompilationConstantValue(CompilationConstantIntegerKind left, CompilationConstantIntegerKind right)
        {
            left.LogicalAnd(right);
            return left;
        }

        public override ICompilationValue CompilationValue(CompilationBuilder builder, CompilationValue left, CompilationValue right)
        {
            return builder.LogicalAnd(left, right);
        }
    }
}



