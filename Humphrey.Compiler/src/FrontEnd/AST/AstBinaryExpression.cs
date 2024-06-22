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
                    expression = new AstBinaryReference(left, right);
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
                    if (right != null)
                        throw new Exception($"Right hand side of post increment should be null");
                    expression = new AstUnaryPostIncrement(left);
                    right = left;
                    break;
                case "--":
                    if (right != null)
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
                    expression = new AstFunctionCall(left, right);
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
                case "<<":
                    expression = new AstBinaryLogicalShiftLeft(left, right);
                    break;
                case ">>":
                    expression = new AstBinaryLogicalShiftRight(left, right);
                    break;
                case ">>>":
                    expression = new AstBinaryArithmeticShiftRight(left, right);
                    break;
                default:
                    throw new NotImplementedException($"Unimplemented binary operator : {oper.Dump()}");
            }

            expression.Token = new Result<Tokens>(left.Token.Value, left.Token.Location, right.Token.Remainder);
            return expression;
        }

        public static (CompilationValue lhs, CompilationValue rhs) FixupBinaryExpressionInputs(CompilationUnit unit, CompilationBuilder builder, CompilationValue left, CompilationValue right, Result<Tokens> token)
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

            var leftFunction = left.Type as CompilationFunctionType;
            var rightFunction = right.Type as CompilationFunctionType;

            if (leftFunction != null && rightFunction != null)
            {
                if (leftFunction.Same(rightFunction))
                    return (left, right);
                else
                    throw new Exception($"TODO - Error incompatable function");
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

                    unit.Messages.Log(CompilerErrorKind.Error_SignedUnsignedMismatch, $"Result of expression '{token.Location.ToStringValue(token.Remainder)}' of type '{leftIntType.DumpType()}' is same width but signedness of type does not match {rightIntType.DumpType()}!", token.Location, token.Remainder);
                    return (left, right);
                }

                if (leftIntType.IntegerWidth < rightIntType.IntegerWidth)
                {
                    return (builder.Ext(left, rightIntType), right);
                }
                if (rightIntType.IntegerWidth < leftIntType.IntegerWidth)
                {
                    return (left, builder.Ext(right, leftIntType));
                }

                throw new NotImplementedException($"TODO - Integer Bit width does not match");
            }

            var leftFloatType = left.Type as CompilationFloatType;
            var rightFloatType = right.Type as CompilationFloatType;

            if (leftFloatType != null && rightFloatType != null)
            {
                return (left, right);
            }

            throw new NotImplementedException($"TODO - Non integer types in promotion?");
        }

        public static IType ResolveExpressionType(SemanticPass pass, IType left, IType right, Result<Tokens> token)
        {
            var resolvedLeft = left.ResolveBaseType(pass);
            var resolvedRight = right.ResolveBaseType(pass);
            while (true)
            {
                if (resolvedLeft is AstStructureType lst)
                {
                    if (lst.Elements.Length == 1)
                    {
                        if (lst.Elements[0].NumElements == 1)
                        {
                            left = lst.Elements[0].Type;
                            resolvedLeft = left.ResolveBaseType(pass);
                            continue;
                        }
                    }
                }
                if (resolvedRight is AstStructureType rst)
                {
                    if (rst.Elements.Length == 1)
                    {
                        if (rst.Elements[0].NumElements == 1)
                        {
                            right = rst.Elements[0].Type;
                            resolvedRight = right.ResolveBaseType(pass);
                            continue;
                        }
                    }
                }
                break;
            }

            var lP = resolvedLeft as AstPointerType;
            var rP = resolvedRight as AstPointerType;

            if (lP != null && rP != null)
            {
                if (lP.ElementType.Dump() == rP.ElementType.Dump())
                    return left;
                else
                {
                    pass.Messages.Log(CompilerErrorKind.Error_TypeMismatch, $"Type mismatch : '{lP.ElementType.Dump()}' != '{rP.ElementType.Dump()}'", token.Location, token.Remainder);
                    return left;
                }
            }

            var lE = resolvedLeft as AstEnumType;
            var rE = resolvedRight as AstEnumType;

            if (lE != null)
            {
                resolvedLeft = lE.Type.ResolveBaseType(pass);
            }
            if (rE != null)
            {
                resolvedRight = rE.Type.ResolveBaseType(pass);
            }

            if (resolvedLeft.GetType() != resolvedRight.GetType())
            {
                if (resolvedLeft.GetType()==typeof(AstArrayType) && resolvedRight.GetType()==typeof(AstBitType))
                    return left;
                if (resolvedRight.GetType()==typeof(AstArrayType) && resolvedLeft.GetType()==typeof(AstBitType))
                    return right;

                if (resolvedLeft.GetType() == typeof(AstFp32Type) && resolvedRight.GetType() == typeof(AstBitType))
                    return left;
                if (resolvedRight.GetType() == typeof(AstFp32Type) && resolvedLeft.GetType() == typeof(AstBitType))
                    return right;
                if (resolvedLeft.GetType() == typeof(AstFp32Type) && resolvedRight.GetType() == typeof(AstArrayType))
                {
                    var rightArray = resolvedRight as AstArrayType;
                    if (rightArray.ElementType.GetType() == typeof(AstBitType))
                        return left;
                }
                if (resolvedRight.GetType() == typeof(AstFp32Type) && resolvedLeft.GetType() == typeof(AstArrayType))
                {
                    var leftArray = resolvedLeft as AstArrayType;
                    if (leftArray.ElementType.GetType() == typeof(AstBitType))
                        return right;
                }

                pass.Messages.Log(CompilerErrorKind.Error_TypeMismatch, $"Type mismatch : '{left.Token.Value}' != '{right.Token.Value}", token.Location, token.Remainder);
            }

            return left;
        }
    }
}

