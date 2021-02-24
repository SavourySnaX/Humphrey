using System;
using Humphrey.Backend;

namespace Humphrey.FrontEnd
{
    public static class AstBinaryExpression
    {
        public static IExpression FetchBinaryExpressionRhsIdentifer(IOperator oper, IExpression left, AstIdentifier right)
        {
            IExpression expression = default;
            switch (oper.Dump())
            {
                case ".":
                    expression = new AstBinaryReference(left,right);
                    break;
                default:
                    throw new NotImplementedException($"Unimplemented binary operator rhs identifer : {oper.Dump()}");
            }
            expression.Token = new Result<Tokens>(left.Token.Value, left.Token.Location, right.Token.Remainder);
            return expression;
        }

        public static IExpression FetchBinaryExpressionRhsExpressionContinuation(IOperator oper, IExpression left, IExpression right)
        {
            IExpression expression = default;
            switch (oper.Dump())
            {
                case "[":
                    expression = new AstArraySubscript(left, right);
                    break;
                case "++":
                    if (right!=null)
                        throw new Exception($"Right hand side of post increment should be null");
                    expression = new AstUnaryPostIncrement(left);
                    right = left;
                    break;
                case "--":
                    if (right!=null)
                        throw new Exception($"Right hand side of post decrement should be null");
                    expression = new AstUnaryPostDecrement(left);
                    right = left;
                    break;
                default:
                    throw new NotImplementedException($"Unimplemented binary operator rhs identifer : {oper.Dump()}");
            }
            expression.Token = new Result<Tokens>(left.Token.Value, left.Token.Location, right.Token.Remainder);
            return expression;
        }

        public static IExpression FetchBinaryExpressionRhsExpressionList(IOperator oper, IExpression left, AstExpressionList right)
        {
            IExpression expression = default;
            switch (oper.Dump())
            {
                case "(":
                    expression = new AstFunctionCall(left,right);
                    break;
                default:
                    throw new NotImplementedException($"Unimplemented binary operator rhs identifer : {oper.Dump()}");
            }

            expression.Token = new Result<Tokens>(left.Token.Value, left.Token.Location, right.Token.Remainder);
            return expression;
        }

        public static IExpression FetchBinaryExpressionRhsType(IOperator oper, IExpression left, IType right)
        {
            IExpression expression = default;
            switch (oper.Dump())
            {
                case "as":
                    expression = new AstBinaryAs(left, right);
                    break;
                default:
                    throw new NotImplementedException($"Unimplemented binary operator rhs type : {oper.Dump()}");
            }
            expression.Token = new Result<Tokens>(left.Token.Value, left.Token.Location, right.Token.Remainder);
            return expression;
        }

        public static IExpression FetchBinaryExpression(IOperator oper, IExpression left, IExpression right)
        {
            IExpression expression = default;
            switch (oper.Dump())
            {
                case "+":
                    expression = new AstBinaryPlus(left, right);
                    break;
                case "-":
                    expression = new AstBinaryMinus(left, right);
                    break;
                case "*":
                    expression = new AstBinaryMultiply(left, right);
                    break;
                case "/":
                    expression = new AstBinaryDivide(left, right);
                    break;
                case "%":
                    expression = new AstBinaryModulus(left, right);
                    break;
                case "==":
                    expression = new AstBinaryCompareEqual(left, right);
                    break;
                case "!=":
                    expression = new AstBinaryCompareNotEqual(left, right);
                    break;
                case "<=":
                    expression = new AstBinaryCompareLessEqual(left, right);
                    break;
                case ">=":
                    expression = new AstBinaryCompareGreaterEqual(left, right);
                    break;
                case "<":
                    expression = new AstBinaryCompareLess(left, right);
                    break;
                case ">":
                    expression = new AstBinaryCompareGreater(left, right);
                    break;
                case "&":
                    expression = new AstBinaryBinaryAnd(left, right);
                    break;
                case "|":
                    expression = new AstBinaryBinaryOr(left, right);
                    break;
                case "^":
                    expression = new AstBinaryBinaryXor(left, right);
                    break;
                case "&&":
                    expression = new AstBinaryLogicalAnd(left, right);
                    break;
                case "||":
                    expression = new AstBinaryLogicalOr(left, right);
                    break;
                default:
                    throw new NotImplementedException($"Unimplemented binary operator : {oper.Dump()}");
            }

            expression.Token = new Result<Tokens>(left.Token.Value, left.Token.Location, right.Token.Remainder);
            return expression;
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

            var leftPointer = left.Type as CompilationPointerType;
            var rightPointer = right.Type as CompilationPointerType;

            if (leftPointer != null && rightPointer != null)
            {
                if (leftPointer.Same(rightPointer))
                    return (left, right);
                else
                    throw new Exception($"TODO - Error incompatable pointer");
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

