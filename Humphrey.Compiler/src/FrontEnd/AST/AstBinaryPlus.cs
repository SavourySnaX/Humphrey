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

        public override ICompilationConstantValue CompilationConstantValue(ICompilationConstantValue left, ICompilationConstantValue right)
        {
            if (left is CompilationConstantFloatKind lfi && right is CompilationConstantIntegerKind rfi)
            {
                right = rfi.AsFloat();
            }
            if (left is CompilationConstantIntegerKind lik && right is CompilationConstantFloatKind rfk)
            {
                left = lik.AsFloat();
            }
            if (left is CompilationConstantIntegerKind li && right is CompilationConstantIntegerKind ri)
            {
                li.Add(ri);
                return li;
            }
            if (left is CompilationConstantFloatKind lf && right is CompilationConstantFloatKind rf)
            {
                lf.Add(rf);
                return lf;
            }
            throw new CompilationAbortException("Invalid types for addition");
        }

        public override ICompilationValue CompilationValue(CompilationBuilder builder, CompilationValue left, CompilationValue right)
        {
            if (left.Type is CompilationFloatType)
                return builder.FAdd(left, right);
            return builder.Add(left, right);
        }
    }
}