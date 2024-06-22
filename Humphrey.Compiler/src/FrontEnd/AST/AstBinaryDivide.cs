using Humphrey.Backend;

namespace Humphrey.FrontEnd
{
    public class AstBinaryDivide : AstBinaryExpressionExpression
    {
        public AstBinaryDivide(IExpression left, IExpression right) : base(left,right)
        {
        }
    
        public override string DumpOperator()
        {
            return "/";
        }

        public override CompilationConstantIntegerKind CompilationConstantValue(CompilationConstantIntegerKind left, CompilationConstantIntegerKind right)
        {
            left.Div(right);
            return left;
        }

        public override ICompilationValue CompilationValue(CompilationBuilder builder, CompilationValue left, CompilationValue right)
        {
            if (left.Type is CompilationFloatType)
                return builder.FDiv(left, right);

            var leftIntType = left.Type as CompilationIntegerType;
            var rightIntType = right.Type as CompilationIntegerType;

            if (leftIntType.IsSigned || rightIntType.IsSigned)
                return builder.SDiv(left, right);

            return builder.UDiv(left, right);
        }
    }
}



