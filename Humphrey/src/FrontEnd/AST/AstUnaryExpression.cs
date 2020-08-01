using System;
using Humphrey.Backend;
namespace Humphrey.FrontEnd
{
    public static class AstUnaryExpression 
    {
        public static IExpression FetchUnaryExpression(IOperator oper, IExpression expression)
        {
            switch (oper.Dump())
            {
                case "+":
                    return new AstUnaryPlus(expression);
                case "-":
                    return new AstUnaryMinus(expression);
                case "*":
                    return new AstUnaryDereference(expression);
                default:
                    throw new NotImplementedException($"Unimplemented unary operator : {oper.Dump()}");
            }
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
                    if (cst.Elements.Length == 1)
                    {
                        src = cst.LoadElement(unit, builder, src, cst.Elements[0].Identifier);
                        continue;
                    }
                }
                break;
            }

            // Always promote integer type to largest of two sizes if not matching is the current rule..
            var srcIntType = src.Type as CompilationIntegerType;
            var destIntType = destType as CompilationIntegerType;
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


