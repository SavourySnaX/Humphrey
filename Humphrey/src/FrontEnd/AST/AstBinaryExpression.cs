using System;
using Humphrey.Backend;

namespace Humphrey.FrontEnd
{
    public static class AstBinaryExpression
    {
        public static IExpression FetchBinaryExpressionRhsIdentifer(IOperator oper, IExpression left, AstIdentifier right)
        {
            switch (oper.Dump())
            {
                case ".":
                    return new AstBinaryReference(left,right);
                default:
                    throw new NotImplementedException($"Unimplemented binary operator rhs identifer : {oper.Dump()}");
            }
        }

        public static IExpression FetchBinaryExpressionRhsExpressionContinuation(IOperator oper, IExpression left, IExpression right)
        {
            switch (oper.Dump())
            {
                case "[":
                    return new AstArraySubscript(left,right);
                default:
                    throw new NotImplementedException($"Unimplemented binary operator rhs identifer : {oper.Dump()}");
            }
        }

        public static IExpression FetchBinaryExpressionRhsExpressionList(IOperator oper, IExpression left, AstExpressionList right)
        {
            switch (oper.Dump())
            {
                case "(":
                    return new AstFunctionCall(left,right);
                default:
                    throw new NotImplementedException($"Unimplemented binary operator rhs identifer : {oper.Dump()}");
            }
        }

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
                case "==":
                    return new AstBinaryCompareEqual(left, right);
                case "!=":
                    return new AstBinaryCompareNotEqual(left, right);
                case "<=":
                    return new AstBinaryCompareLessEqual(left, right);
                case ">=":
                    return new AstBinaryCompareGreaterEqual(left, right);
                case "<":
                    return new AstBinaryCompareLess(left, right);
                case ">":
                    return new AstBinaryCompareGreater(left, right);
                case "&":
                    return new AstBinaryBinaryAnd(left, right);
                case "|":
                    return new AstBinaryBinaryOr(left, right);
                case "^":
                    return new AstBinaryBinaryXor(left, right);
                case "&&":
                    return new AstBinaryLogicalAnd(left, right);
                case "||":
                    return new AstBinaryLogicalOr(left, right);
                default:
                    throw new NotImplementedException($"Unimplemented binary operator : {oper.Dump()}");
            }
        }

        public static (CompilationValue lhs, CompilationValue rhs) FixupBinaryExpressionInputs(CompilationUnit unit, CompilationBuilder builder, CompilationValue left, CompilationValue right)
        {
            while (true)
            {
                // Special flexible case if a structure has a single element, we can directly unpack (this may need to be in a loop)
                {
                    if (left.Type is CompilationStructureType cst)
                    {
                        if (cst.Fields.Length == 1)
                        {
                            left = cst.LoadElement(unit, builder, left, cst.Fields[0]);
                            continue;
                        }
                    }
                }
                // Special flexible case if a structure has a single element, we can directly unpack (this may need to be in a loop)
                {
                    if (right.Type is CompilationStructureType cst)
                    {
                        if (cst.Fields.Length == 1)
                        {
                            right = cst.LoadElement(unit, builder, right, cst.Fields[0]);
                            continue;
                        }
                    }
                }
                break;
            }

            var leftEnum = left.Type as CompilationEnumType;
            var rightEnum = right.Type as CompilationEnumType;

            // Always promote integer type to largest of two sizes if not matching is the current rule..
            var leftIntType = left.Type as CompilationIntegerType;
            var rightIntType = right.Type as CompilationIntegerType;

            if (leftEnum != null)
                leftIntType = leftEnum.ElementType as CompilationIntegerType;
            if (rightEnum != null)
                rightIntType = rightEnum.ElementType as CompilationIntegerType;

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

                if (leftIntType.IntegerWidth<rightIntType.IntegerWidth)
                {
                    return (builder.Ext(left, rightIntType), right);
                }
                if (rightIntType.IntegerWidth<leftIntType.IntegerWidth)
                {
                    return (left, builder.Ext(right, leftIntType));
                }

                throw new NotImplementedException($"TODO - Integer Bit width does not match");
            }

            throw new NotImplementedException($"TODO - Non integer types in promotion?");
        }
    }


}
