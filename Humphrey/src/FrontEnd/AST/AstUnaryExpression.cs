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
            CompilationValue src = default;
            if (result is CompilationConstantValue constantResult)
            {
                src = constantResult.GetCompilationValue(unit, destType);
            }
            else
                src = result as CompilationValue;

            // Always promote integer type to largest of two sizes if not matching is the current rule..
            if (src.Type.IsIntegerType == true && destType.IsIntegerType == true)
            {
                if (src.Type.IntegerWidth == destType.IntegerWidth)
                {
                    if (src.Type.IsSigned == destType.IsSigned)
                    {
                        return src;
                    }

                    throw new NotImplementedException($"TODO - signed/unsigned mismatch");
                }
                else if (src.Type.IntegerWidth < destType.IntegerWidth)
                {
                    // For integers, if the the size is strictly less, then the assignment is always allowed (we just upcast the value to the new bitwidth)
                    return builder.Ext(src, destType);
                }

                throw new NotImplementedException($"TODO - Integer Bit width does not match");
            }
            if (src.Type == destType)
                return src;

            throw new NotImplementedException($"TODO - Non integer types in promotion?");
        }
    }
}


