using Humphrey.Backend;

namespace Humphrey.FrontEnd
{
    public class AstBinaryPlus : AstBinaryExpressionExpression
    {
        public AstBinaryPlus(IExpression left, IExpression right) : base(left, right)
        {
        }
    
        public override string DumpOperator()
        {
            return "+";
        }

        public override CompilationConstantIntegerKind CompilationConstantValue(CompilationConstantIntegerKind left, CompilationConstantIntegerKind right)
        {
            left.Add(right);
            return left;
        }

        public override ICompilationValue CompilationValue(CompilationBuilder builder, CompilationValue left, CompilationValue right)
        {
            if (left.Type is CompilationFloatType)
                return builder.FAdd(left, right);
            return builder.Add(left, right);
        }
    }
}