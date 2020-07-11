using System;
using Humphrey.Backend;

namespace Humphrey.FrontEnd
{
    public static class AstBinaryExpression
    {
        public static IExpression FetchBinaryExpression(IOperator oper, IExpression left, IExpression right)
        {
            switch (oper.Dump())
            {
                case "+":
                    return new AstBinaryPlus(left, right);
                case "-":
                    return new AstBinaryMinus(left, right);
                case "*":
                    return new AstBinaryMultiply(left, right);
                case "/":
                    return new AstBinaryDivide(left, right);
                case "%":
                    return new AstBinaryModulus(left, right);
                default:
                    throw new NotImplementedException($"Unimplemented binary operator : {oper.Dump()}");
            }
        }

        public static (CompilationValue lhs, CompilationValue rhs) FixupBinaryExpressionInputs(CompilationUnit unit, CompilationBuilder builder, CompilationValue left, CompilationValue right)
        {
            // Always promote integer type to largest of two sizes if not matching is the current rule..
            if (left.Type.IsIntegerType == true && right.Type.IsIntegerType == true)
            {
                if (left.Type.IntegerWidth == right.Type.IntegerWidth)
                {
                    if (left.Type.IsSigned == right.Type.IsSigned)
                    {
                        return (left, right);
                    }

                    throw new NotImplementedException($"TODO - signed/unsigned mismatch");
                }

                throw new NotImplementedException($"TODO - Integer Bit width does not match");
            }

            throw new NotImplementedException($"TODO - Non integer types in promotion?");
        }
    }


}

