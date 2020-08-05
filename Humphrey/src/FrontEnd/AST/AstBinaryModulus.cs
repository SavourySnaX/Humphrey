using Humphrey.Backend;

namespace Humphrey.FrontEnd
{
    public class AstBinaryModulus : AstBinaryExpressionExpression
    {
        public AstBinaryModulus(IExpression left, IExpression right) : base(left, right)
        {
        }
    
        public override string DumpOperator()
        {
            return "%";
        }

        public override CompilationConstantValue CompilationConstantValue(CompilationConstantValue left, CompilationConstantValue right)
        {
            left.Rem(right);
            return left;
        }

        public override ICompilationValue CompilationValue(CompilationBuilder builder, CompilationValue left, CompilationValue right)
        {
            var leftIntType = left.Type as CompilationIntegerType;
            var rightIntType = right.Type as CompilationIntegerType;

            if (leftIntType.IsSigned || rightIntType.IsSigned)
                return builder.SRem(left, right);

            return builder.URem(left, right);
        }
    }
}




