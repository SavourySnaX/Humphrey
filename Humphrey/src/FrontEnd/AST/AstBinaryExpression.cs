using System;
using Humphrey.Backend;

namespace Humphrey.FrontEnd
{
    public static class AstBinaryExpression
    {
        public static IExpression FetchBinaryExpressionRhsType(IOperator oper, IExpression left, IType right)
        {
            switch (oper.Dump())
            {
                case "as":
                    return new AstBinaryAs(left, right);
                default:
                    throw new NotImplementedException($"Unimplemented binary operator rhs type : {oper.Dump()}");
            }
            
        }
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
            var leftIntType = left.Type as CompilationIntegerType;
            var rightIntType = right.Type as CompilationIntegerType;
            if (leftIntType != null && rightIntType != null)
            {
                if (leftIntType.IntegerWidth == rightIntType.IntegerWidth)
                {
                    if (leftIntType.IsSigned == rightIntType.IsSigned)
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

