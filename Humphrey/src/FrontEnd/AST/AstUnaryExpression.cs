using System;
using Humphrey.Backend;
namespace Humphrey.FrontEnd
{
    public static class AstUnaryExpression 
    {
        public static IExpression FetchUnaryExpression(IOperator oper, IExpression expression)
        {
            IExpression unary = default;
            switch (oper.Dump())
            {
                case "+":
                    unary = new AstUnaryPlus(expression);
                    break;
                case "-":
                    unary = new AstUnaryMinus(expression);
                    break;
                case "*":
                    unary = new AstUnaryDereference(expression);
                    break;
                case "!":
                    unary = new AstUnaryLogicalNot(expression);
                    break;
                case "~":
                    unary = new AstUnaryBinaryNot(expression);
                    break;
                case "++":
                    unary = new AstUnaryPreIncrement(expression);
                    break;
                case "--":
                    unary = new AstUnaryPreDecrement(expression);
                    break;
                default:
                    throw new NotImplementedException($"Unimplemented unary operator : {oper.Dump()}");
            }
            unary.Token = expression.Token;
            return unary;
        }
        public static CompilationValue EnsureTypeOk(CompilationUnit unit, CompilationBuilder builder, IExpression expr, CompilationType destType)
        {
            var result = expr.ProcessExpression(unit, builder);
            CompilationValue src = Expression.ResolveExpressionToValue(unit, result, destType);

            while (true)
            {
                if (src.Type.Same(destType))
                    return src;

                // Special flexible case if a structure has a single element, we can directly unpack (this may need to be in a loop)
                if (src.Type is CompilationStructureType cst)
                {
                    if (cst.Fields.Length == 1)
                    {
                        src = cst.LoadElement(unit, builder, src, cst.Fields[0]);
                        continue;
                    }
                }
                break;
            }
            var srcEnum = src.Type as CompilationEnumType;
            var destEnum = destType as CompilationEnumType;

            // Always promote integer type to largest of two sizes if not matching is the current rule..
            var srcIntType = src.Type as CompilationIntegerType;
            var destIntType = destType as CompilationIntegerType;

            if (srcEnum != null)
            {
                srcIntType = srcEnum.ElementType as CompilationIntegerType;
                src = new CompilationValue(src.BackendValue, srcIntType);
            }
            if (destEnum != null)
                destIntType = destEnum.ElementType as CompilationIntegerType;

            if (srcIntType != null && destIntType != null)
            {
                if (srcIntType.IntegerWidth == destIntType.IntegerWidth)
                {
                    if (srcIntType.IsSigned == destIntType.IsSigned)
                    {
                        return src;
                    }

                    throw new NotImplementedException($"TODO - signed/unsigned mismatch");
                }
                else if (srcIntType.IntegerWidth < destIntType.IntegerWidth)
                {
                    // For integers, if the the size is strictly less, then the assignment is always allowed (we just upcast the value to the new bitwidth)
                    return builder.Ext(src, destType);
                }

                throw new NotImplementedException($"TODO - Integer Bit width does not match");
            }
            throw new NotImplementedException($"TODO - Non integer types in promotion?");
        }
    }
}


